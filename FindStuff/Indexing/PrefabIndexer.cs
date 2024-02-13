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
                    SubFilter.Parking
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
                    SubFilter.Surface
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

                if ( _prefabSearchQueue.TryDequeue( out var id ) )
                {
                    WorkingSet.Add( id, ( search, orderByAscending ) =>
                    {
                        return StringSearch( search, id ) || WordSearch( search, id );
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
                .GroupBy( p => Enum.Parse<Filter>( p.Type ) )
                .ToDictionary( g => g.Key, g => g.Select( p => p.ID ).ToArray( ) );

            _filterPrefabs = new( filterPrefabs );

        }

        private void CacheSubFilterPrefabs( )
        {
            var subFilterPrefabs = _prefabs.Values
                .Where( IsSubFilterPrefab )
                .GroupBy( p => Enum.Parse<SubFilter>( p.Type ) )
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
        /// Break down the search into words and find matches
        /// </summary>
        /// <param name="search"></param>
        /// <param name="prefabID"></param>
        /// <returns></returns>
        private bool WordSearch( string search, int prefabID )
        {
            if ( string.IsNullOrEmpty( search ) )
                return false;

            var words = search.Split( ' ', StringSplitOptions.RemoveEmptyEntries );

            if ( words?.Length > 0 )
            {
                foreach ( var word in words )
                {
                    if ( StringSearch( word, prefabID ) )
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Search a prefab's info for a string
        /// </summary>
        /// <param name="search"></param>
        /// <param name="prefabID"></param>
        /// <returns></returns>
        private bool StringSearch( string search, int prefabID )
        {
            if ( string.IsNullOrEmpty( search ) )
                return false;

            var name = _prefabNames[prefabID].ToLowerInvariant( );
            var displayName = _prefabDisplayNames[prefabID].ToLowerInvariant( );
            var typeName = _prefabTypesNames[prefabID].ToLowerInvariant( );
            var tags = _prefabTags[prefabID];

            return name.Contains( search ) ||
                displayName.Contains( search ) ||
                typeName.Contains( search ) ||
                tags.Count( t => t.Contains( search ) ) > 0;
        }

        /// <summary>
        /// Add prefabs to the working set, filtering them by search if necessary.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="prefabIDs"></param>
        private void AddToWorkingSet( FindStuffViewModel model, int[] prefabIDs )
        {
            if ( prefabIDs == null || prefabIDs.Length == 0 )
                return;

            var hasSearch = !string.IsNullOrEmpty( model.Search );

            foreach ( var id in prefabIDs )
            {
                if ( WorkingSet.Data.Contains( id ) )
                    continue;

                if ( hasSearch )
                {
                    var search = model.Search.ToLowerInvariant( ).Trim( );

                    if ( StringSearch( search, id ) || WordSearch( search, id ) )
                        WorkingSet.Data.Add( id );
                    // else don't include in results
                }
                else
                    WorkingSet.Data.Add( id );
            }
        }

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

            if ( WorkingSet.Parameters.OrderByAscending )
            {
                WorkingSet.Data = WorkingSet.Data
                    .OrderBy( id => _prefabNames[id].ToLowerInvariant( ) )
                    .ToHashSet( );
            }
            else
            {
                WorkingSet.Data = WorkingSet.Data
                    .OrderByDescending( id => _prefabNames[id].ToLowerInvariant( ) )
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
                           .Select( id => _prefabs[id] )
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
                Parameters = (model.Search == null ? "" : model.Search.ToLowerInvariant( ).Trim( ), model.OrderByAscending)
            };

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
                            .OrderBy( p => ResolvePrefabName( _localisationManager, p.Name ) )
                            .Select( p => p.ID )
                            .ToHashSet( );
                    }
                    else
                    {
                        WorkingSet.Data = _prefabs.Values
                            .OrderByDescending( p => ResolvePrefabName( _localisationManager, p.Name ) )
                            .Select( p => p.ID )
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
