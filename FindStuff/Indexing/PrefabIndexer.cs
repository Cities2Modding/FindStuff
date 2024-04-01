using Colossal.IO.AssetDatabase.Internal;
using Colossal.Localization;
using FindStuff.UI;
using Game.Prefabs;
using Game.SceneFlow;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace FindStuff.Indexing
{
    /// <summary>
    /// Index prefabs allowing for quicker searching and filtering.
    /// </summary>
    /// <param name="prefabs"></param>
    /// <param name="localisationManager"></param>
    public class PrefabIndexer : MonoBehaviour
    {
        static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            DefaultValueHandling = DefaultValueHandling.Populate,
            NullValueHandling = NullValueHandling.Include,
            Converters = new[]
            {
                new Newtonsoft.Json.Converters.StringEnumConverter( )
            }
        };

        public static readonly Dictionary<Filter, SubFilter[]> _filterMappings = new( )
        {
            {
                Filter.Foliage,
                [
                    SubFilter.Tree,
                    SubFilter.Plant
                ]
            },
            {
                Filter.Buildings,
                [
                    SubFilter.ServiceBuilding,
                    SubFilter.SignatureBuilding,
                    SubFilter.Park,
                    SubFilter.Parking,
                    SubFilter.MiscBuilding
                ]
            },
            {
                Filter.Zones,
                [
                    SubFilter.ZoneResidential,
                    SubFilter.ZoneCommercial,
                    SubFilter.ZoneIndustrial,
                    SubFilter.ZoneOffice
                ]
            },
            {
                Filter.Props,
                [
                    SubFilter.Billboards,
                    SubFilter.Fences,
                    SubFilter.SignsAndPosters,
                    SubFilter.Accessory,
                    SubFilter.PropMisc
                ]
            },
            {
                Filter.Network,
                [
                    SubFilter.RoadTool,
                    SubFilter.SmallRoad,
                    SubFilter.MediumRoad,
                    SubFilter.LargeRoad,
                    SubFilter.Highway,
                    SubFilter.Roundabout,
                    SubFilter.Pavement,
                    SubFilter.OtherNetwork
                ]
            },
            {
                Filter.Misc,
                [
                    SubFilter.Surface,
                    SubFilter.Area,
                    SubFilter.TransportStop
                ]
            }
        };

        private static readonly LocalizationManager _localisationManager = GameManager.instance.localizationManager;

        private ReadOnlyDictionary<int, PrefabItem> _prefabs;
        private ReadOnlyDictionary<int, string> _prefabNames;
        private ReadOnlyDictionary<int, string> _prefabTypesNames;
        private ReadOnlyDictionary<int, string> _prefabDisplayNames;
        private ReadOnlyDictionary<int, HashSet<string>> _prefabTags;

        private readonly HashSet<Filter> _filters = [.. ( ( Filter[] ) Enum.GetValues( typeof( Filter ) ) )];
        private readonly HashSet<SubFilter> _subFilters = [.. ( ( SubFilter[] ) Enum.GetValues( typeof( SubFilter ) ) )];

        private ReadOnlyDictionary<Filter, int[]> _filterPrefabs;
        private ReadOnlyDictionary<SubFilter, int[]> _subFilterPrefabs;
        private HashSet<int> _favouritePrefabs;
        private Stopwatch _stopWatch = new Stopwatch( );

        private WorkingSearchData WorkingSet
        {
            get;
            set;
        }

        private string LastWorkingSetKey
        {
            get;
            set;
        }

        private Dictionary<string, (string Json, int Count)> BigQueryCache
        {
            get;
            set;
        } = new Dictionary<string, (string Json, int Count)>( );

        public bool QueryInProgress
        {
            get
            {
                return WorkingSet != null;
            }
        }

        private int _searchFrameBatches = 128;

        private Queue<int> _prefabSearchQueue = new Queue<int>( );
        
        public static readonly PrefabIndexer _instance;

        static PrefabIndexer( )
        {
            if ( _instance != null )
                return;

            var existing = GameObject.Find( "FindStuff_PrefabIndexer" );

            if ( existing != null )
                Destroy( existing );

            var newGameObject = new GameObject( "FindStuff_PrefabIndexer" );
            DontDestroyOnLoad( newGameObject );

            _instance = newGameObject.AddComponent<PrefabIndexer>( );
        }

        private void Start( )
        {
        }

        private void Update( )
        {
            if ( !_prefabSearchQueue.Any( ) )
            {
                return;
            }

            StartCoroutine( ProcessSearchQueue( ) );
        }

        private IEnumerator ProcessSearchQueue( )
        {
            if ( _prefabSearchQueue.Count == 0 || WorkingSet == null )
                yield return null;

            var searchCount = Math.Min( _searchFrameBatches, _prefabSearchQueue.Count );

            for ( var i = 0; i < searchCount; i++ )
            {
                if ( WorkingSet == null ) // It was cancelled mid-loop
                    yield return null;

                if ( _prefabSearchQueue.Any() )
                {
                    var id = _prefabSearchQueue.Dequeue( );
                    WorkingSet.Add( id, ( search, orderByAscending ) =>
                    {
                        var stringSearch = StringSearch( search, id );
                        var wordSearch = (false, int.MaxValue );//WordSearch( search, id );
                        return (stringSearch.IsMatch || wordSearch.Item1, ( stringSearch.IsMatch ? stringSearch.Score : 0 ) + ( wordSearch.Item1 ? wordSearch.MaxValue : 0 ));
                    } );
                }

                if ( i >= _prefabSearchQueue.Count - 1 ) // It's traversed all results
                    break;
            }

            if ( _prefabSearchQueue.Count == 0 && WorkingSet != null ) // We've finished the queue and not cancelled
            {
                ProcessOrderBy( );
                GatherResult( );
                PresentResults( );
            }

            yield return null;
        }

        private void PresentResults( )
        {
            if ( WorkingSet != null )
            {
                //UnityEngine.Debug.Log( $"Finished search query: {WorkingSet.Key} results: {WorkingSet.Cache.Count}" );
                WorkingSet.OnComplete?.Invoke( WorkingSet.Key, WorkingSet.Cache );
                ClearWorkingSet( );
            }
        }

        /// <summary>
        /// Check if a prefab is a filter prefab
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private bool IsFilterPrefab( PrefabItem prefab )
        {
            if ( !Enum.TryParse<Filter>( prefab.Type, out var filter ) )
                return false;

            return _filters.Contains( filter );
        }

        /// <summary>
        /// Check if a prefab is a sub filter prefab
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        private bool IsSubFilterPrefab( PrefabItem prefab )
        {
            if ( !Enum.TryParse<SubFilter>( prefab.Type, out var subFilter ) )
                return false;

            return _subFilters.Contains( subFilter );
        }

        private void CacheFilterPrefabs( )
        {
            var filterPrefabs = _prefabs.Values
                .Where( IsFilterPrefab )
                .GroupBy( p => ( Filter ) Enum.Parse( typeof( Filter ), p.Type ) )
                .ToDictionary( g => g.Key, g => g.Select( p => p.ID ).ToArray( ) );

            _filterPrefabs = new( filterPrefabs );

        }

        private void CacheSubFilterPrefabs( )
        {
            var subFilterPrefabs = _prefabs.Values
                .Where( IsSubFilterPrefab )
                .GroupBy( p => ( SubFilter ) Enum.Parse( typeof( SubFilter ), p.Type ) )
                .ToDictionary( g => g.Key, g => g.Select( p => p.ID ).ToArray( ) );

            _subFilterPrefabs = new( subFilterPrefabs );
        }

        /// <summary>
        /// Build the indexes
        /// </summary>
        public void Build( List<PrefabItem> prefabs )
        {
            _stopWatch.Restart( );

            _prefabs = new( prefabs.ToDictionary( p => p.ID, p => p ) );
            _prefabNames = new( _prefabs.ToDictionary( p => p.Key, p => p.Value.Name ) );
            _prefabTypesNames = new( _prefabs.ToDictionary( p => p.Key, p => p.Value.Type ) );
            _prefabDisplayNames = new( _prefabs.ToDictionary( p => p.Key, p => ResolvePrefabName( _localisationManager, p.Value.Name ) ) );
            _prefabTags = new( _prefabs.ToDictionary( p => p.Key, p => p.Value.Tags.Distinct( ).Select( t => ResolveTagName( _localisationManager, t.ToLowerInvariant( ) ).ToLowerInvariant( ) ).ToHashSet( ) ) );

            CacheFilterPrefabs( );
            CacheSubFilterPrefabs( );

            _stopWatch.Stop( );
            UnityEngine.Debug.Log( $"[PrefabIndexer] Build took {_stopWatch.ElapsedMilliseconds} ms." );
        }

        /// <summary>
        /// Clear the working set
        /// </summary>
        private void ClearWorkingSet( )
        {
            WorkingSet = null;
            _prefabSearchQueue.Clear();
        }

        /// <summary>
        /// Get a prefab and its filters by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public (PrefabItem Prefab, Filter Filter, SubFilter SubFilter) GetPrefab( int id )
        {
            if ( !_prefabs.ContainsKey( id ) )
                return default;

            var prefab = _prefabs[id];
            var filter = Filter.None;
            var subFilter = SubFilter.None;

            // If the prefab type is a valid top level filter
            if ( !string.IsNullOrEmpty( prefab.Type ) &&
                Enum.TryParse<Filter>( prefab.Type, out var f ) &&
                !_filterMappings.ContainsKey( f ) )
            {
                filter = f;
            }
            else if ( !string.IsNullOrEmpty( prefab.Category ) &&
                Enum.TryParse<Filter>( prefab.Category, out var f2 ) &&
                Enum.TryParse<SubFilter>( prefab.Type, out var sf ) &&
                _filterMappings.ContainsKey( f2 ) )
            {
                var mappings = _filterMappings[f2];

                if ( mappings.Contains( sf ) )
                {
                    filter = f2;
                    subFilter = sf;
                }
            }

            return (prefab, filter, subFilter);
        }

        /// <summary>
        /// Get a prefab and its filters by name
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public (PrefabItem Prefab, Filter Filter, SubFilter SubFilter) GetPrefab( string name )
        {
            var id = _prefabNames.FirstOrDefault( kvp => kvp.Value == name ).Key;

            if ( !_prefabs.ContainsKey( id ) )
                return default;

            var prefab = _prefabs[id];
            var filter = Filter.None;
            var subFilter = SubFilter.None;

            // If the prefab type is a valid top level filter
            if ( !string.IsNullOrEmpty( prefab.Type ) &&
                Enum.TryParse<Filter>( prefab.Type, out var f ) &&
                !_filterMappings.ContainsKey( f ) )
            {
                filter = f;
            }
            else if ( !string.IsNullOrEmpty( prefab.Category ) &&
                Enum.TryParse<Filter>( prefab.Category, out var f2 ) &&
                Enum.TryParse<SubFilter>( prefab.Type, out var sf ) &&
                _filterMappings.ContainsKey( f2 ) )
            {
                var mappings = _filterMappings[f2];

                if ( mappings.Contains( sf ) )
                {
                    filter = f2;
                    subFilter = sf;
                }
            }

            return (prefab, filter, subFilter);
        }

        public PrefabItem GetByName( string name )
        {
            return _prefabs.Select( kvp => kvp.Value ).FirstOrDefault( n => n.Name == name );
        }


        /// <summary>
        /// Computes the Levenshtein distance between two strings, optimized for memory usage.
        /// Treats null strings as empty strings.
        /// </summary>
        /// <param name="source">The source string to compare from, treated as empty if null.</param>
        /// <param name="target">The target string to compare to, treated as empty if null.</param>
        /// <returns>The minimum number of single-character edits required to change the source into the target.</returns>
        public int LevenshteinDistance( string source, string target )
        {
            // Treat null strings as empty strings.
            source ??= string.Empty;
            target ??= string.Empty;

            // Ensure the source string is shorter than the target string to optimize space.
            if ( source.Length > target.Length )
            {
                var temp = source;
                source = target;
                target = temp;
            }

            int sourceLength = source.Length;
            int targetLength = target.Length;
            int[] previousRowDistances = new int[sourceLength + 1];
            int[] currentRowDistances = new int[sourceLength + 1];

            for ( int i = 0; i <= sourceLength; i++ )
                previousRowDistances[i] = i;

            for ( int targetIndex = 1; targetIndex <= targetLength; targetIndex++ )
            {
                currentRowDistances[0] = targetIndex;

                for ( int sourceIndex = 1; sourceIndex <= sourceLength; sourceIndex++ )
                {
                    int cost = ( source[sourceIndex - 1] == target[targetIndex - 1] ) ? 0 : 1;
                    currentRowDistances[sourceIndex] = Math.Min(
                        Math.Min( currentRowDistances[sourceIndex - 1] + 1, previousRowDistances[sourceIndex] + 1 ),
                        previousRowDistances[sourceIndex - 1] + cost );
                }

                var temp = previousRowDistances;
                previousRowDistances = currentRowDistances;
                currentRowDistances = temp;
            }

            return previousRowDistances[sourceLength];
        }

        /// <summary>
        /// Break down the search into words and find matches
        /// </summary>
        /// <param name="search"></param>
        /// <param name="prefabID"></param>
        /// <returns></returns>
        private (bool IsMatch, int Score) WordSearch( string search, int prefabID )
        {
            if ( string.IsNullOrEmpty( search ) )
                return (false, int.MinValue);

            var words = search.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
            var hasMatch = false;

            var score = int.MinValue;

            if ( words?.Length > 0 )
            {
                score = 0;

                foreach ( var word in words )
                {
                    var stringSearch = StringSearch( word, prefabID );

                    // Word searches are less influential than top level searches
                    if ( stringSearch.IsMatch )
                    {
                        score += ( stringSearch.Score * 100 );
                        hasMatch = true;
                    }
                }
            }

            return (hasMatch, score);
        }

        /// <summary>
        /// Search a prefab's info for a string
        /// </summary>
        /// <param name="search"></param>
        /// <param name="prefabID"></param>
        /// <returns></returns>
        private (bool IsMatch, int Score) StringSearch(string search, int prefabID)
        {
            if (string.IsNullOrEmpty(search))
                return (false, int.MaxValue);

            search = search.ToLowerInvariant();

            var name = _prefabNames[prefabID].ToLowerInvariant();
            var displayName = _prefabDisplayNames[prefabID].ToLowerInvariant();
            var typeName = _prefabTypesNames[prefabID].ToLowerInvariant();
            var tags = _prefabTags[prefabID];

            if ( displayName == search || displayName == search.Replace( " ", "" ) || displayName == search.Replace( "-", "" )
                || name == search  || name == search.Replace( " ", "" ) || name == search.Replace( "-", "" ) )
                return (true, int.MinValue);

            var hasMatches = false;
            var curScore = 0;

            if ( displayName.Contains( search ) )
            {
                hasMatches = true;
                curScore += LevenshteinDistance( displayName, search );
            }
            else
                curScore += LevenshteinDistance( displayName, search ) * 10;

            if ( name.Contains( search ) )
            {
                hasMatches = true;
                curScore += LevenshteinDistance( name, search ) * 2;
            }
            else
                curScore += LevenshteinDistance( name, search ) * 20;

            if ( typeName.Contains( search ) )
            {
                hasMatches = true;
                curScore += LevenshteinDistance( typeName, search ) * 3;
            }
            else
                curScore += LevenshteinDistance( typeName, search ) * 30;

            var searchWords = search.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
            var displayNameWords = displayName.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );

            if ( searchWords?.Length > 1 && displayNameWords.Length > 1 )
            {
                hasMatches = displayNameWords.Sum( w => searchWords.Count( sw => w.Contains( sw ) ) ) >= searchWords.Length;
               
                var subWordScores = new List<int>( );

                foreach ( var displayNameWord in displayNameWords )
                {
                    foreach ( var searchWord in searchWords )
                    {
                        if ( searchWord == displayNameWord )
                            subWordScores.Add( 0 );
                        else
                            subWordScores.Add( LevenshteinDistance( displayNameWord, searchWord ) );
                    }
                }

                curScore += (subWordScores.Sum( ) * 15);
                curScore = Math.Max( curScore, int.MinValue );
            }

            var matchingTags = tags.Where( t => t.Contains( search ) )
                .Select( t => t == search ? 0 : LevenshteinDistance( t, search ));

            if ( matchingTags.Any( ) )
            {
                hasMatches = true;
                curScore += matchingTags.Sum( ) * 40;
            }

            return (hasMatches, !hasMatches ? int.MaxValue : curScore);
        }
        //private (bool IsMatch, int Score) StringSearch( string search, int prefabID )
        //{
        //    if ( string.IsNullOrEmpty( search ) )
        //        return (false, int.MinValue);

        //    var name = _prefabNames[prefabID].ToLowerInvariant( );
        //    var displayName = _prefabDisplayNames[prefabID].ToLowerInvariant( );
        //    var typeName = _prefabTypesNames[prefabID].ToLowerInvariant( );
        //    var tags = _prefabTags[prefabID];

        //    // If it's an exact match give it the best score (0)
        //    //if ( name == search || displayName == search ||
        //    //     name == search.Replace( " ", "" ) )
        //     //   return (true, 0);

        //    var nameContains = name.Contains( search );
        //    var displayNameContains = displayName.Contains( search );
        //    var typeNameContains = typeName.Contains( search );

        //    var nameDistance = /*!nameContains ? 1000 :*/ LevenshteinDistance( name, search );
        //    var displayNameDistance = /*!displayNameContains ? 1000 : */LevenshteinDistance( displayName, search );
        //    var typeNameDistance = /*!typeNameContains ? 50 :*/ LevenshteinDistance( typeName, search );

        //    var totalDistance = ( nameDistance + displayNameDistance + typeNameDistance );

        //    if ( search.Length >= 6 && totalDistance < 30 )
        //        UnityEngine.Debug.Log( $"Leven: name = {name}, totalDistance = {totalDistance}, nameDistance = {nameDistance}, displayNameDistance = {displayNameDistance}, typeNameDistance = {typeNameDistance}" );
            
        //    var matchingTags = tags.Where( t => t.Contains( search ) );

        //    var tagsDistance = 0;

        //    if ( matchingTags.Any( ) )
        //    {
        //        foreach ( var matchingTag in matchingTags )
        //        {
        //            tagsDistance += LevenshteinDistance( matchingTag, search );
        //        }
        //    }
        //    else
        //        tagsDistance = 100;

        //    var score = nameDistance + displayNameDistance + typeNameDistance + tagsDistance;
        //    return (nameContains || displayNameContains || typeNameContains || matchingTags.Any(), score);
        //}

        ///// <summary>
        ///// Add prefabs to the working set, filtering them by search if necessary.
        ///// </summary>
        ///// <param name="model"></param>
        ///// <param name="prefabIDs"></param>
        //private void AddToWorkingSet( FindStuffViewModel model, int[] prefabIDs )
        //{
        //    if ( prefabIDs == null || prefabIDs.Length == 0 )
        //        return;

        //    var hasSearch = !string.IsNullOrEmpty( model.Search );

        //    foreach ( var id in prefabIDs )
        //    {
        //        if ( WorkingSet.Data.Count( d => d.ID == id ) > 0 )
        //            continue;

        //        if ( hasSearch )
        //        {
        //            var search = model.Search.ToLowerInvariant( ).Trim( );
        //            var stringSearch = StringSearch( search, id );
        //            var wordSearch = WordSearch( search, id );

        //            if ( stringSearch.IsMatch || wordSearch.IsMatch )
        //                WorkingSet.Data.Add( (id, ( stringSearch.IsMatch ? stringSearch.Score : 0 ) + ( wordSearch.IsMatch ? wordSearch.Score : 0 )) );
        //            // else don't include in results
        //        }
        //        else
        //            WorkingSet.Data.Add( (id, 0) );
        //    }
        //}

        /// <summary>
        /// Update the favourites list
        /// </summary>
        /// <param name="model"></param>
        private void UpdateFavourites( FindStuffViewModel model )
        {
            _favouritePrefabs = _prefabNames
                .Where( kvp => model.Favourites.Count( f => f == kvp.Value ) > 0 )
                .Select( kvp => kvp.Key )
                .ToHashSet( );
        }

        /// <summary>
        /// Filter the working set by ordering mechanism
        /// </summary>
        /// <param name="model"></param>
        private void ProcessOrderBy( )
        {
            if ( WorkingSet == null || WorkingSet.Data == null )
                return;

            if ( !string.IsNullOrEmpty( WorkingSet.CurrentSearch ) )
            {
                WorkingSet.Data = WorkingSet.Data
                    .OrderBy( d => d.Score )
                    .ToHashSet( );
            }
            else if ( WorkingSet.Parameters.OrderByAscending )
            {
                WorkingSet.Data = WorkingSet.Data
                    .OrderBy( d => _prefabDisplayNames[d.ID].ToLowerInvariant( ) )
                    .ToHashSet( );
            }
            else
            {
                WorkingSet.Data = WorkingSet.Data
                    .OrderByDescending( d => _prefabDisplayNames[d.ID].ToLowerInvariant( ) )
                    .ToHashSet( );
            }
        }

        /// <summary>
        /// Generate a key for a working set
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string GenerateKey( FindStuffViewModel model )
        {
            return $"{model.Filter}:{model.SubFilter}:{model.Search ?? ""}:{model.OrderByAscending.ToString( ).ToLower( )}{( model.Filter == Filter.Favourite ? ":" + model.Favourites.Count : "" )}";
        }

        private void GatherResult( )
        {
            if ( WorkingSet == null || WorkingSet.Data == null )
                return;

            var result = new PrefabQueryResult
            {
                Prefabs = WorkingSet.Data
                           .Select( d => _prefabs[d.ID] )
                           .ToArray( )
            };

            WorkingSet.Cache = (JsonConvert.SerializeObject( result, _jsonSettings ), result.Prefabs == null ? 0 : result.Prefabs.Length);

            // If the query is big and there's no search filter, add to the big query cache
            if ( WorkingSet.Cache.Count > 250
                && string.IsNullOrEmpty( WorkingSet.Parameters.Search )
                && !BigQueryCache.ContainsKey( WorkingSet.Key ) )
            {
                BigQueryCache.Add( WorkingSet.Key, WorkingSet.Cache );
            }
        }

        private void UpdateSpeed( SearchSpeed speed )
        {
            switch ( speed )
            {
                case SearchSpeed.VeryLow:
                    _searchFrameBatches = 50;
                    break;

                case SearchSpeed.Low:
                    _searchFrameBatches = 100;
                    break;

                case SearchSpeed.Medium:
                    _searchFrameBatches = 250;
                    break;

                case SearchSpeed.High:
                    _searchFrameBatches = 500;
                    break;

                case SearchSpeed.VeryHigh:
                    _searchFrameBatches = 1_000;
                    break;
            }
        }

        /// <summary>
        /// Query prefab items based on the state of the model
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="model"></param>
        /// <param name="onComplete"></param>
        /// <param name="onShowLoader"></param>
        /// <returns></returns>
        public void Query( SearchSpeed speed, FindStuffViewModel model, Action<string, (string Json, int Count)> onComplete, Action onShowLoader )
        {
            if ( WorkingSet != null )
            {
                //UnityEngine.Debug.Log( $"Cancelled previous query!" );
                CancelQuery( );
            }

            UpdateSpeed( speed );
            //_stopWatch.Restart( );

            var hasInstantResult = true;

            var workingSetKey = GenerateKey( model );

            // Only execute a query if the inputs changed
            if ( LastWorkingSetKey == workingSetKey )
                return;

            LastWorkingSetKey = workingSetKey;

            WorkingSet = new WorkingSearchData
            {
                Key = workingSetKey,
                OnComplete = onComplete,
                SearchWords = model.SearchWords,
                SearchTags = model.SearchTags,
                CurrentSearch = model.Search,
                Parameters = (model.Search == null ? "" : model.Search.ToLowerInvariant( ).Trim( ), model.OrderByAscending)
            };

            UnityEngine.Debug.Log( "Search:" + model.Search );
            var hasTriggeredLoader = false;

            if ( !BigQueryCache.ContainsKey( workingSetKey ) || !string.IsNullOrEmpty( model.Search ) || model.Filter == Filter.Favourite )
            {
                if ( model.Filter == Filter.None &&
                    model.SubFilter == SubFilter.None &&
                    string.IsNullOrEmpty( model.Search ) )
                {
                    if ( !hasTriggeredLoader && _prefabs.Count >= _searchFrameBatches )
                    {
                        hasTriggeredLoader = true;
                        onShowLoader?.Invoke( );
                    }

                    if ( model.OrderByAscending )
                    {
                        WorkingSet.Data = _prefabs.Values
                            .OrderBy( p => _prefabDisplayNames[p.ID].ToLowerInvariant( ) )
                            .Select( p => (p.ID, 0) )
                            .ToHashSet( );
                    }
                    else
                    {
                        WorkingSet.Data = _prefabs.Values
                            .OrderByDescending( p => _prefabDisplayNames[p.ID].ToLowerInvariant( ) )
                            .Select( p => (p.ID, 0) )
                            .ToHashSet( );
                    }
                }
                // We use more efficient means to search to reduce overhead
                else
                {
                    hasInstantResult = false;

                    var filterIsPrefab = _filterPrefabs.ContainsKey( model.Filter );

                    switch ( model.Filter )
                    {
                        case Filter.None:
                            if ( !hasTriggeredLoader && _prefabs.Count >= _searchFrameBatches )
                            {
                                hasTriggeredLoader = true;
                                onShowLoader?.Invoke( );
                            }

                            foreach ( var id in _prefabs.Keys )
                                _prefabSearchQueue.Enqueue( id );
                            break;

                        case Filter.Favourite:
                            UpdateFavourites( model );

                            if ( !hasTriggeredLoader && _favouritePrefabs.Count >= _searchFrameBatches )
                            {
                                hasTriggeredLoader = true;
                                onShowLoader?.Invoke( );
                            }

                            if ( _favouritePrefabs?.Count > 0 )
                            {
                                foreach ( var id in _favouritePrefabs )
                                    _prefabSearchQueue.Enqueue( id );
                            }
                            break;

                        default:
                            if ( filterIsPrefab )
                            {
                                var prefabIDs = _filterPrefabs[model.Filter];

                                if ( !hasTriggeredLoader && prefabIDs.Length >= _searchFrameBatches )
                                {
                                    hasTriggeredLoader = true;
                                    onShowLoader?.Invoke( );
                                }

                                foreach ( var id in prefabIDs )
                                    _prefabSearchQueue.Enqueue( id );
                            }
                            break;
                    }

                    var canSubFilter = model.Filter != Filter.None &&
                        model.Filter != Filter.Favourite;

                    if ( !filterIsPrefab
                        && canSubFilter )
                    {
                        switch ( model.SubFilter )
                        {
                            // If none ensure if a filter is applied all children are included
                            case SubFilter.None:
                                if ( _filterMappings.ContainsKey( model.Filter ) )
                                {
                                    var filterChildren = _filterMappings[model.Filter];

                                    if ( filterChildren?.Length > 0 )
                                    {
                                        foreach ( var subFilter in filterChildren )
                                        {
                                            if ( _subFilterPrefabs.ContainsKey( subFilter ) )
                                            {
                                                var prefabIDs = _subFilterPrefabs[subFilter];

                                                if ( !hasTriggeredLoader && prefabIDs.Length >= _searchFrameBatches )
                                                {
                                                    hasTriggeredLoader = true;
                                                    onShowLoader?.Invoke( );
                                                }

                                                foreach ( var id in prefabIDs )
                                                    _prefabSearchQueue.Enqueue( id );
                                            }
                                        }
                                    }
                                }
                                break;

                            default:
                                if ( _subFilterPrefabs.ContainsKey( model.SubFilter ) )
                                {
                                    var prefabIDs = _subFilterPrefabs[model.SubFilter];

                                    if ( !hasTriggeredLoader && prefabIDs.Length >= _searchFrameBatches )
                                    {
                                        hasTriggeredLoader = true;
                                        onShowLoader?.Invoke( );
                                    }

                                    foreach ( var id in prefabIDs )
                                        _prefabSearchQueue.Enqueue( id );
                                }
                                break;
                        }
                    }

                    //UnityEngine.Debug.Log( $"Enqueued {WorkingSet.Key}: {_prefabSearchQueue.Count}" );
                }

                if ( hasInstantResult )
                {
                    GatherResult( );
                    PresentResults( );
                }
            }
            // get from big query cache
            else
            {
                WorkingSet.Cache = BigQueryCache[WorkingSet.Key];

                if ( WorkingSet.Cache.Count >= 512 )
                    onShowLoader?.Invoke( );

                PresentResults( );
            }

            //_stopWatch.Stop( );

            //UnityEngine.Debug.Log( $"[PrefabIndexer] Query '{WorkingSetKey}' with '{WorkingSetCache.Count}' results took {_stopWatch.ElapsedMilliseconds} ms." );
        }

        /// <summary>
        /// Resolve a prefab name from the localisation system.
        /// </summary>
        /// <param name="localizationManager"></param>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        private static string ResolvePrefabName( LocalizationManager localizationManager, string prefabName )
        {
            var localeKey = "Assets.NAME[" + prefabName + "]";

            if ( localizationManager.activeDictionary.TryGetValue( localeKey, out var localisedName ) )
            {
                return localisedName;
            }

            return prefabName;
        }

        private static string ResolveTagName( LocalizationManager localizationManager, string tag )
        {
            var localeKey = "FindStuff.Tag." + tag;

            if ( localizationManager.activeDictionary.TryGetValue( localeKey, out var localisedName ) )
            {
                return localisedName;
            }

            return tag;
        }

        public void CancelQuery( )
        {
            if ( !QueryInProgress )
                return;

            ClearWorkingSet( );
        }
    }

    public class PrefabQueryResult
    {
        public PrefabItem[] Prefabs
        {
            get;
            set;
        }
    }
}
