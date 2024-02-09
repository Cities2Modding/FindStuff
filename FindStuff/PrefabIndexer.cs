using Colossal.Localization;
using FindStuff.UI;
using Game.SceneFlow;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using static Game.UI.NameSystem;
using Unity.Entities.UniversalDelegates;

namespace FindStuff
{
    /// <summary>
    /// Index prefabs allowing for quicker searching and filtering.
    /// </summary>
    /// <param name="prefabs"></param>
    /// <param name="localisationManager"></param>
    public class PrefabIndexer( List<PrefabItem> prefabs )
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

        private readonly ReadOnlyDictionary<int, PrefabItem> _prefabs = new( prefabs.ToDictionary( p => p.ID, p => p ) );
        private readonly ReadOnlyDictionary<int, string> _prefabNames = new( prefabs.ToDictionary( p => p.ID, p => p.Name ) );
        private readonly ReadOnlyDictionary<int, string> _prefabTypesNames = new( prefabs.ToDictionary( p => p.ID, p => p.Type ) );
        private readonly ReadOnlyDictionary<int, string> _prefabDisplayNames = new( prefabs.ToDictionary( p => p.ID, p => ResolvePrefabName( _localisationManager, p.Name ) ) );
        private readonly ReadOnlyDictionary<int, HashSet<string>> _prefabTags = new( prefabs.ToDictionary( p => p.ID, p => p.Tags.Distinct().Select( t => ResolveTagName( _localisationManager, t.ToLowerInvariant() ).ToLowerInvariant( ) ).ToHashSet()) );

        private readonly HashSet<Filter> _filters = [.. ( ( Filter[] ) Enum.GetValues( typeof( Filter ) ) )];
        private readonly HashSet<SubFilter> _subFilters = [.. ( ( SubFilter[] ) Enum.GetValues( typeof( SubFilter ) ) )];
        
        private ReadOnlyDictionary<Filter, int[]> _filterPrefabs;
        private ReadOnlyDictionary<SubFilter, int[]> _subFilterPrefabs;
        private HashSet<int> _favouritePrefabs;
        private Stopwatch _stopWatch = new Stopwatch();

        private HashSet<int> WorkingSet
        {
            get;
            set;
        } = [];

        private (string Json, int Count) WorkingSetCache
        {
            get;
            set;
        }

        private string WorkingSetKey
        {
            get;
            set;
        }

        private Dictionary<string, (string Json, int Count)> BigQueryCache
        {
            get;
            set;
        } = new Dictionary<string, (string Json, int Count)>( );

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
        public void Build( )
        {
            _stopWatch.Restart( );

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
            WorkingSet.Clear( );
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
                if ( WorkingSet.Contains( id ) )
                    continue;

                if ( hasSearch )
                {
                    var search = model.Search.ToLowerInvariant( ).Trim( );
                    var name = _prefabNames[id].ToLowerInvariant( );
                    var displayName = _prefabDisplayNames[id].ToLowerInvariant( );
                    var typeName = _prefabTypesNames[id].ToLowerInvariant( );
                    var tags = _prefabTags[id];

                    if ( name.Contains( search ) ||
                        displayName.Contains( search ) ||
                        typeName.Contains( search ) ||
                        tags.Count( t => t.Contains( search ) ) > 0 )
                    {
                        WorkingSet.Add( id );
                    }
                }
                else
                    WorkingSet.Add( id );
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
        private void ProcessOrderBy( FindStuffViewModel model )
        {
            if ( model.OrderByAscending )
            {
                WorkingSet = WorkingSet
                    .OrderBy( id => _prefabNames[id].ToLowerInvariant( ) )
                    .ToHashSet( );
            }
            else
            {
                WorkingSet = WorkingSet
                    .OrderByDescending( id => _prefabNames[id].ToLowerInvariant( ) )
                    .ToHashSet( );
            }
        }

        /// <summary>
        /// Generate a key for a working set
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private string GenerateKey( FindStuffViewModel model )
        {
            return $"{model.Filter}:{model.SubFilter}:{model.Search ?? ""}:{model.OrderByAscending.ToString().ToLower()}{( model.Filter == Filter.Favourite ? ":" + model.Favourites.Count : "" )}";
        }

        /// <summary>
        /// Query prefab items based on the state of the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public (string Json, int Count) Query( FindStuffViewModel model, out string key )
        {
            //_stopWatch.Restart( );

            ClearWorkingSet( );

            var workingSetKey = GenerateKey( model );

            if ( !BigQueryCache.ContainsKey( workingSetKey ) || !string.IsNullOrEmpty( model.Search ) || model.Filter == Filter.Favourite )
            {
                // Only execute a query if the inputs changed
                if ( WorkingSetKey != workingSetKey )
                {
                    WorkingSetKey = workingSetKey;

                    if ( model.Filter == Filter.None &&
                        model.SubFilter == SubFilter.None &&
                        string.IsNullOrEmpty( model.Search ) &&
                        model.OrderByAscending )
                    {
                        WorkingSet = _prefabs.Values
                            .OrderBy( p => ResolvePrefabName( _localisationManager, p.Name ) )
                            .Select( p => p.ID )
                            .ToHashSet( );
                    }
                    else
                    {
                        var filterIsPrefab = _filterPrefabs.ContainsKey( model.Filter );

                        switch ( model.Filter )
                        {
                            case Filter.None:
                                AddToWorkingSet( model, _prefabs.Keys.ToArray() );
                                break;

                            case Filter.Favourite:
                                UpdateFavourites( model );
                                if ( _favouritePrefabs?.Count > 0 )
                                    AddToWorkingSet( model, _favouritePrefabs.ToArray( ) );
                                break;

                            default:
                                if ( filterIsPrefab )
                                {
                                    var prefabIDs = _filterPrefabs[model.Filter];
                                    AddToWorkingSet( model, prefabIDs );
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
                                                    AddToWorkingSet( model, prefabIDs );
                                                }
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    if ( _subFilterPrefabs.ContainsKey( model.SubFilter ) )
                                    {
                                        var prefabIDs = _subFilterPrefabs[model.SubFilter];
                                        AddToWorkingSet( model, prefabIDs );
                                    }
                                    break;
                            }
                        }

                        ProcessOrderBy( model );
                    }

                    var result = new PrefabQueryResult
                    {
                        Prefabs = WorkingSet
                            .Select( id => _prefabs[id] )
                            .ToArray( )
                    };

                    WorkingSetCache = (JsonConvert.SerializeObject( result, _jsonSettings ), result.Prefabs == null ? 0 : result.Prefabs.Length );

                    // If the query is big and there's no search filter, add to the big query cache
                    if ( WorkingSetCache.Count > 250
                        && string.IsNullOrEmpty( model.Search )
                        && !BigQueryCache.ContainsKey( WorkingSetKey ) )
                    {
                        BigQueryCache.Add( WorkingSetKey, WorkingSetCache );
                    }
                }
            }
            // It's in big query cache and not the last working set so
            // get from big query cache
            else if ( workingSetKey != WorkingSetKey )
            {
                WorkingSetKey = workingSetKey;
                WorkingSetCache = BigQueryCache[WorkingSetKey];
            }

            //_stopWatch.Stop( );

            //UnityEngine.Debug.Log( $"[PrefabIndexer] Query '{WorkingSetKey}' with '{WorkingSetCache.Count}' results took {_stopWatch.ElapsedMilliseconds} ms." );

            key = WorkingSetKey;
            return WorkingSetCache;
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
