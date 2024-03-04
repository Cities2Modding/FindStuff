using Colossal.Localization;
using Colossal.Serialization.Entities;
using FindStuff.Configuration;
using FindStuff.Helper;
using FindStuff.Indexing;
using FindStuff.Systems;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Gooee.Plugins;
using Gooee.Plugins.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FindStuff.UI
{
    [ControllerDepends( SystemUpdatePhase.ToolUpdate, typeof( PickerToolSystem ) )]
    public class FindStuffController : Controller<FindStuffViewModel>
    {
        private bool IsPickingShortcut
        {
            get;
            set;
        }

        public bool IsPicking
        {
            get
            {
                return Model.IsPicking;
            }
        }

        public bool EnableShortcut
        {
            get
            {
                return Model.EnableShortcut;
            }
        }

        private ToolSystem _toolSystem;
        private ToolRaycastSystem _toolRaycastSystem;
        private DefaultToolSystem _defaulToolSystem;
        private PickerToolSystem _pickerToolSystem;
        private PrefabSystem _prefabSystem;
        private ImageSystem _imageSystem;
        private ToolbarUISystem _toolbarUISystem;
        private PloppableRICOSystem _ploppableRICOSystem;
        private InputAction _enableAction;
        private TerrainSystem _terrainSystem;

        static FieldInfo _prefabsField = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private Dictionary<string, PrefabBase> _prefabInstances = [];
        private EntityArchetype _entityArchetype;
        private EndFrameBarrier _endFrameBarrier;
        static MethodInfo _selectAsset = typeof( ToolbarUISystem ).GetMethods( BindingFlags.Instance | BindingFlags.NonPublic )
            .FirstOrDefault( m => m.Name == "SelectAsset" && m.GetParameters( ).Length == 1 );

        public readonly static FindStuffConfig _config = ConfigBase.Load<FindStuffConfig>( );

        private IBaseHelper[] _baseHelper;
        private PrefabIndexer _indexer;
        private LocalizationManager _localizationManager;

        private static HashSet<string> TypesWithNoThumbnails = ["Surface", "PropMisc", "Billboards", "Fences", "SignsAndPosters", "Accessory"];
        private static HashSet<string> ModdedComponents = ["CustomSurface", "CustomDecal", "CustomSurfaceComponent"];

        private float _lastSearchTime;
        private string _lastSearch;

        private string _lastQueryKey = "";

        private Queue<(string Key, string Json)> _queryResults = new Queue<(string Key, string Json)>( );

        private FindStuffSettings _modSettings;

        public override FindStuffViewModel Configure( )
        {
            _modSettings = ( FindStuffSettings ) Settings;

            SetupResourceHandler( );

            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>( );
            _toolRaycastSystem = World.GetExistingSystemManaged<ToolRaycastSystem>( );
            _defaulToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>( );
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _imageSystem = World.GetOrCreateSystemManaged<ImageSystem>( );
            _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>( );
            _toolbarUISystem = World.GetOrCreateSystemManaged<ToolbarUISystem>( );
            _pickerToolSystem = World.GetOrCreateSystemManaged<PickerToolSystem>( );
            _ploppableRICOSystem = World.GetOrCreateSystemManaged<PloppableRICOSystem>( );
            _terrainSystem = World.GetExistingSystemManaged<TerrainSystem>( );
            _localizationManager = GameManager.instance.localizationManager;

            _entityArchetype = this.EntityManager.CreateArchetype( ComponentType.ReadWrite<Unlock>( ), ComponentType.ReadWrite<Game.Common.Event>( ) );

            var model = new FindStuffViewModel( );

            model.Favourites = _config.Favourites.ToList( ); // Create a copy
            model.ViewMode = _config.ViewMode;
            model.Filter = _config.Filter;
            model.SubFilter = _config.SubFilter;
            model.OrderByAscending = _config.OrderByAscending;
            model.EnableShortcut = _config.EnableShortcut;
            model.ExpertMode = _config.ExpertMode;
            model.OperationMode = Enum.Parse<ViewOperationMode>( _modSettings.OperationMode );

            if ( _config.RecentSearches == null )
                _config.RecentSearches = [];

            model.RecentSearches = _config.RecentSearches
                .OrderByDescending( s => s.Value )
                .Select( s => s.Key )
                .ToHashSet( );

            return model;
        }

        private bool CheckForModdedComponents( Entity entity )
        {
            var components = EntityManager.GetComponentTypes( entity )
                .Select( ct => ct.GetManagedType( ).FullName ).ToList( );

            if ( components?.Count > 0 )
            {
                foreach ( var moddedComponent in ModdedComponents )
                {
                    if ( components.Count( c => c.ToLowerInvariant( ).EndsWith( moddedComponent.ToLowerInvariant( ) ) ) > 0 )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void OnGameLoadingComplete( Purpose purpose, GameMode mode )
        {
            base.OnGameLoadingComplete( purpose, mode );

            if ( mode == GameMode.Game )
            {
                var plugin = Plugin as FindStuffPlugin;

                if ( plugin.Settings is FindStuffSettings settings )
                    _modSettings = settings;

                UpdateFromSettings( );

                if ( _enableAction != null )
                {
                    _enableAction.Disable( );
                    _enableAction.Dispose( );
                }

                _enableAction = new InputAction( "FindStuff_Toggle" );
                _enableAction.AddCompositeBinding( "ButtonWithOneModifier" )
                    .With( "Modifier", "<Keyboard>/CTRL" )
                    .With( "Button", "<Keyboard>/f" );
                _enableAction.performed += ( a ) => OnToggleVisible( );
                _enableAction.Enable( );

                _enableAction = new InputAction( "FindStuff_Escape" );
                _enableAction.AddBinding( "<Keyboard>/escape" );
                _enableAction.performed += ( a ) => OnEscapePressed( );
                _enableAction.Enable( );

                var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
                UnityEngine.Debug.Log( $"Getting {prefabs.Count} prefabs" );

                var prefabsList = new List<PrefabItem>( );

                _baseHelper =
                [
                    new PlantHelper( EntityManager ),
                    new TreeHelper( EntityManager ),
                    new SurfaceHelper( EntityManager ),
                    new AreaHelper( EntityManager ),
                    new CityServiceHelper( EntityManager ),
                    new NetworkHelper( EntityManager ),
                    new SignatureBuildingHelper( EntityManager ),
                    new BuildingHelper( EntityManager ),
                    new VehicleHelper( EntityManager ),
                    new ZoneBuildingHelper( EntityManager, _prefabSystem ),
                    new PropHelper( EntityManager ),
                ];

                foreach ( var prefabBase in prefabs )
                {
                    // Skip potentially crashing prefabs
                    if ( GetEvilPrefabs.Contains( prefabBase.name.ToLower( ) ) )
                        continue;

                    //if ( prefabBase.name.ToLower( ).Contains( "alley" ) )
                    //{
                    //    var components = EntityManager.GetComponentTypes( _prefabSystem.GetEntity( prefabBase ) );

                    //    var componentsList = components.Select( c => c.GetManagedType( ).FullName ).ToList( );
                    //    components.Dispose( );
                    //    UnityEngine.Debug.Log( $"\n{prefabBase.name}: {string.Join( ", ", componentsList )}\n" );
                    //}

                    if ( !ProcessPrefab( prefabBase, out var prefabType, out var categoryType,
                        out var tags, out var meta, out var thumbnailOverride, out var isExpertMode ) )
                        continue;

                    var prefabIcon = "";
                    var entity = _prefabSystem.GetEntity( prefabBase );
                    var thumbnail = _imageSystem.GetThumbnail( entity );
                    var typeIcon = GetTypeIcon( prefabType, prefabBase, entity );

                    if ( thumbnail == null || thumbnail == _imageSystem.placeholderIcon )
                        prefabIcon = typeIcon;
                    else
                        prefabIcon = thumbnail;

                    var prefabItem = new PrefabItem
                    {
                        ID = entity.Index,
                        Name = prefabBase.name,
                        IsModded = CheckForModdedComponents( entity ),
                        Type = prefabType,
                        Category = categoryType,
                        Thumbnail = !string.IsNullOrEmpty( thumbnailOverride ) ? thumbnailOverride : categoryType == "Zones" ? CheckForZoneIcon( prefabBase, entity ) : TypesWithNoThumbnails.Contains( prefabType ) ? typeIcon : prefabIcon,
                        TypeIcon = typeIcon,
                        Meta = meta,
                        Tags = tags,
                        IsExpertMode = isExpertMode
                    };
                    prefabsList.Add( prefabItem );

                    if ( _prefabInstances.ContainsKey( prefabBase.name ) )
                        _prefabInstances.Remove( prefabBase.name );

                    _prefabInstances.Add( prefabBase.name, prefabBase );
                }

                _indexer = PrefabIndexer._instance;
                _indexer.Build( prefabsList );
                Model.Prefabs = prefabsList;
                TriggerUpdate( );
            }
        }

        private string CheckForZoneIcon( PrefabBase prefabBase, Entity entity )
        {
            var helper = new ZoneBuildingHelper( EntityManager, _prefabSystem );
            var icon = helper.GetZoneTypeIcon( prefabBase, entity );
            return icon;
        }

        protected override void OnDestroy( )
        {
            base.OnDestroy( );
            _enableAction?.Disable( );
            _enableAction?.Dispose( );
        }

        private bool ProcessPrefab( PrefabBase prefab, out string prefabType, out string categoryType, out List<string> tags, out Dictionary<string, object> meta, out string thumbnailOverride, out bool isExpertMode )
        {
            thumbnailOverride = null;
            tags = new List<string>( );
            meta = new Dictionary<string, object>( );

            var prefabEntity = _prefabSystem.GetEntity( prefab );
            bool isValid = false;
            prefabType = "Unknown";
            categoryType = "None";
            isExpertMode = false;

            foreach ( IBaseHelper helper in _baseHelper )
            {
                if ( helper.IsValidPrefab( prefab, prefabEntity ) )
                {
                    isValid = true;
                    tags = helper.CreateTags( prefab, prefabEntity );
                    meta = helper.CreateMeta( prefab, prefabEntity );
                    prefabType = helper.PrefabType;
                    categoryType = helper.CategoryType;
                    isExpertMode = helper.IsExpertMode( prefab, prefabEntity );
                    break;
                }
            }

            // If the prefab is a surface export its texture
            if ( prefab is SurfacePrefab surfacePrefab &&
                EntityManager.HasComponent<RenderedAreaData>( prefabEntity )
                && EntityManager.HasComponent<SurfaceData>( prefabEntity ) &&
                EntityManager.HasComponent<PrefabData>( prefabEntity ) )
            {
                var uiObject = prefab.GetComponent<UIObject>( );

                // Custom ELT surface
                if ( uiObject != null && uiObject.m_Icon?.ToLowerInvariant( ).Contains( "customsurfaces/" ) == true )
                {
                    thumbnailOverride = uiObject.m_Icon;
                }
                else
                    thumbnailOverride = SurfaceExporter.ExportSurface( surfacePrefab );
            }

            return isValid;
        }

        private Entity CreatePrefab( Entity prefabEntity, float3 position )
        {
            var objectData = EntityManager.GetComponentData<ObjectData>( prefabEntity );
            var newDuck = EntityManager.CreateEntity( objectData.m_Archetype );

            var transform = new Game.Objects.Transform( );
            transform.m_Position = position;
            LevelToGround( ref transform );

            EntityManager.SetComponentData( newDuck, new PrefabRef( prefabEntity ) );
            EntityManager.SetComponentData( newDuck, transform );

            return newDuck;
        }

        private void LevelToGround( ref Game.Objects.Transform transform )
        {
            var heightData = _terrainSystem.GetHeightData( true );
            transform.m_Position.y = TerrainUtils.SampleHeight( ref heightData, transform.m_Position );
        }

        public static void SetupResourceHandler( )
        {
            var resourceHandler = ( GameUIResourceHandler ) GameManager.instance.userInterface.view.uiSystem.resourceHandler;

            if ( resourceHandler == null || resourceHandler.HostLocationsMap.ContainsKey( "findstuffui" ) )
            {
                UnityEngine.Debug.LogError( "Failed to setup resource handler for FindStuff." );
                return;
            }

            UnityEngine.Debug.Log( "Setup resource handler for FindStuff." );
            resourceHandler.HostLocationsMap.Add( "findstuffui", new List<string> { ConfigBase.MOD_PATH } );
        }

        public bool IsValidPrefab( PrefabBase prefabBase, Entity entity )
        {
            foreach ( IBaseHelper helper in _baseHelper )
            {
                if ( helper.IsValidPrefab( prefabBase, entity ) )
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsValidPrefab( PrefabBase prefabBase, Entity entity, out IBaseHelper prefabHelper )
        {
            prefabHelper = _baseHelper.FirstOrDefault( );
            foreach ( IBaseHelper helper in _baseHelper )
            {
                if ( helper.IsValidPrefab( prefabBase, entity ) )
                {
                    prefabHelper = helper;
                    return true;
                }
            }

            return false;
        }

        private string GetTypeIcon( string type, PrefabBase prefabBase, Entity entity )
        {
            switch ( type )
            {
                case "Plant":
                    return "Media/Game/Icons/Forest.svg";

                case "Tree":
                    return "Media/Game/Icons/Forest.svg";

                case "Network":
                    return "Media/Game/Icons/Roads.svg";

                case "ServiceBuilding":
                    return "Media/Game/Icons/Services.svg";

                case "SignatureBuilding":
                    return "Media/Game/Icons/ZoneSignature.svg";

                case "MiscBuilding":
                    return "Media/Game/Icons/BuildingLevel.svg";

                case "Zoneable":
                    return "Media/Game/Icons/Zones.svg";

                case "ZoneResidential":
                    return "Media/Game/Icons/ZoneResidential.svg";

                case "ZoneCommercial":
                    return "Media/Game/Icons/ZoneCommercial.svg";

                case "ZoneIndustrial":
                    return "Media/Game/Icons/ZoneIndustrial.svg";

                case "ZoneOffice":
                    return "Media/Game/Icons/ZoneOffice.svg";

                case "Vehicle":
                    return "Media/Game/Icons/Traffic.svg";

                case "Surface":
                case "Area":
                    return "Media/Game/Icons/LotTool.svg";

                case "PropMisc":
                    return "fa:solid-cube";

                case "SignsAndPosters":
                    return "fa:solid-clipboard-user";

                case "Fences":
                    return "fa:solid-xmarks-lines";

                case "Billboards":
                    return "fa:solid-rectangle-ad";

                case "Pavement":
                    return "Media/Game/Icons/Pathways.svg";

                case "SmallRoad":
                    return "Media/Game/Icons/SmallRoad.svg";

                case "MediumRoad":
                    return "Media/Game/Icons/MediumRoad.svg";

                case "LargeRoad":
                    return "Media/Game/Icons/LargeRoad.svg";

                case "Highway":
                    return "Media/Game/Icons/Highways.svg";
                //
                case "RoadTool":
                    return "Media/Game/Icons/RoadsServices.svg";

                case "OtherNetwork":
                    return "Media/Game/Icons/Roads.svg";

                case "Park":
                    return "Media/Game/Icons/ParksAndRecreation.svg";

                case "Parking":
                    return "Media/Game/Icons/Parking.svg";

                case "Roundabout":
                    return "Media/Game/Icons/Roundabouts.svg";

                case "Accessory":
                    return "fa:solid-tree-city";
            }

            return "";
        }

        protected override void OnUpdate( )
        {
            base.OnUpdate( );


            //if ( createPrefab != null && _toolRaycastSystem.GetRaycastResult( out var raycastResult ) )
            //{
            //    var prefabEntity = _prefabSystem.GetEntity( createPrefab );
            //    CreatePrefab( prefabEntity, raycastResult.m_Hit.m_Position );

            //    //var mesh = staticObjectPrefab.m_Meshes.FirstOrDefault( );
            //    //var renderPrefab = ( RenderPrefab ) staticObjectPrefab.m_Meshes.FirstOrDefault( ).m_Mesh;

            //    //thumbnailOverride = SurfaceExporter.ExportMeshTexture( renderPrefab );
            //    UnityEngine.Debug.Log( "Try render out asset!!! at " + raycastResult.m_Hit.m_Position );
            //    createPrefab = null;
            //    created = false;
            //}

            if ( !IsPickingShortcut &&
                _toolSystem.activeTool == _defaulToolSystem
                && PickerShortcutTrigger( ) && !IsPicking )
            {
                IsPickingShortcut = true;
                UpdatePicker( true );
            }
            else if ( IsPickingShortcut && !PickerShortcutTrigger( ) && IsPicking )
            {
                IsPickingShortcut = false;
                _pickerToolSystem.RemoveLastHighlighted( );
                UpdatePicker( false );
            }

            if ( _queryResults.Any( ) && _queryResults.TryDequeue( out var result ) )
            {
                GameManager.instance.userInterface.view.View.TriggerEvent( "findstuff.onReceiveResults", result.Key, result.Json );
            }

            if ( !string.IsNullOrEmpty( _lastSearch ) && UnityEngine.Time.time >= _lastSearchTime + 5f )
            {
                IncrementSearchHistory( );

                Model.RecentSearches = _config.RecentSearches
                    .OrderByDescending( s => s.Value )
                    .Select( s => s.Key )
                    .ToHashSet( );

                TriggerUpdate( );

                _lastSearch = null;
            }
        }

        /// <summary>
        /// Increment search history for a specific query, prune searches
        /// not used often.
        /// </summary>
        private void IncrementSearchHistory( )
        {
            if ( _config.RecentSearches == null )
                _config.RecentSearches = [];

            // If we have a full search history and it's been a while since we last purged results
            // then perform a purge of some irrelevant histories
            if ( _config.RecentSearches.Count >= 100 &&
                DateTime.UtcNow >= _config.LastSearchHistoryPurge.AddDays( 1 ) )
            {
                var removalKeys = _config.RecentSearches
                        .OrderBy( r => r.Value )
                        .Select( s => s.Key )
                        .Take( 50 );

                foreach ( var key in removalKeys )
                    _config.RecentSearches.Remove( key );
            }

            if ( !_config.RecentSearches.ContainsKey( _lastSearch ) )
            {
                _config.RecentSearches.Add( _lastSearch, 1 );
            }
            else
            {
                var count = _config.RecentSearches[_lastSearch];

                if ( count < ushort.MaxValue )
                    _config.RecentSearches[_lastSearch] = ( ushort ) ( count + 1 );
            }
            _config.Save( );
        }

        [OnTrigger]
        private void OnTestClick( )
        {
            //TriggerUpdate( );
        }

        [OnTrigger]
        private void OnTogglePicker( )
        {
            UpdatePicker( !Model.IsPicking );
        }

        private void OnEscapePressed( )
        {
            if ( Model.IsVisible || Model.IsPicking )
            {
                // Escape for Pick Stuff
                if ( Model.IsPicking )
                {
                    UpdatePicker( false );
                    _pickerToolSystem.RemoveLastHighlighted( );
                }
                // Escape for Find Stuff
                else
                {
                    OnToggleVisible( );
                }
            }
        }

        [OnTrigger]
        private void OnToggleVisible( )
        {
            Model.IsVisible = !Model.IsVisible;

            // If there's an active tool then ensure stuff is shifted
            if ( Model.OperationMode == ViewOperationMode.MoveFindStuff && Model.IsVisible
                && !Model.Shifted && _toolSystem.activeTool != _defaulToolSystem )
            {
                Model.Shifted = true;
            }

            TriggerUpdate( );
        }

        [OnTrigger]
        private void OnShow( )
        {
            Model.IsVisible = true;
            TriggerUpdate( );
        }

        [OnTrigger]
        private void OnHide( )
        {
            Model.IsVisible = false;
            TriggerUpdate( );
        }

        [OnTrigger]
        private void OnSelectPrefab( string name )
        {
            if ( string.IsNullOrEmpty( name ) )
                return;

            var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
            var prefab = prefabs?.FirstOrDefault( p => p.name == name );

            if ( prefab != null )
            {
                var entity = _prefabSystem.GetEntity( prefab );

                if ( entity != Entity.Null )
                {
                    _toolSystem.ActivatePrefabTool( prefab );

                    // Use barrier to create entity command buffer
                    EntityCommandBuffer commandBuffer = _endFrameBarrier.CreateCommandBuffer( );

                    // Handle zone buildings (spawnable buildings)
                    if ( IsValidPrefab( prefab, entity, out IBaseHelper helper ) && helper is ZoneBuildingHelper )
                    {
                        _ploppableRICOSystem.MakePloppable( entity, commandBuffer );
                    }

                    // Unlock the building is automatic unlocks are enabled
                    if ( _modSettings.AutomaticUnlocks )
                    {
                        var unlockEntity = commandBuffer.CreateEntity( _entityArchetype );
                        commandBuffer.SetComponent( unlockEntity, new Unlock( entity ) );
                    }
                    GameManager.instance.userInterface.view.View.ExecuteScript( $"engine.trigger('toolbar.selectAsset',{{index: {entity.Index}, version: {entity.Version}}});" );
                    //_selectAsset.Invoke( _toolbarUISystem, new object[] { entity } );

                    if ( !created )
                    {
                        createPrefab = prefab;
                        created = true;
                    }
                }
            }
        }
        static bool created = false;
        static PrefabBase createPrefab;

        [OnTrigger]
        private void OnToggleFavourite( string name )
        {
            if ( string.IsNullOrEmpty( name ) )
                return;

            var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
            var prefab = prefabs?.FirstOrDefault( p => p.name == name );

            if ( prefab != null )
            {
                var entity = _prefabSystem.GetEntity( prefab );

                if ( entity != Entity.Null )
                {
                    if ( _config.Favourites.Contains( name ) )
                        _config.Favourites.Remove( name );
                    else
                        _config.Favourites.Add( name );

                    _config.Save( );

                    Model.Favourites = _config.Favourites.ToList( ); // Create a copy
                    TriggerUpdate( );
                }
            }
        }

        protected override void OnModelUpdated( )
        {
            if ( _config.ViewMode != Model.ViewMode ||
                _config.Filter != Model.Filter ||
                _config.SubFilter != Model.SubFilter ||
                _config.ExpertMode != Model.ExpertMode ||
                _config.EnableShortcut != Model.EnableShortcut ||
                _config.OrderByAscending != Model.OrderByAscending )
            {
                _config.ViewMode = Model.ViewMode;
                _config.Filter = Model.Filter;
                _config.SubFilter = Model.SubFilter;
                _config.OrderByAscending = Model.OrderByAscending;
                _config.ExpertMode = Model.ExpertMode;
                _config.EnableShortcut = Model.EnableShortcut;
                _config.Save( );
            }
        }

        protected override void OnSettingsUpdated( )
        {
            UpdateFromSettings( );
        }

        private void UpdateFromSettings( )
        {
            if ( _modSettings == null )
                return;

            var hasUpdate = false;

            if ( _modSettings.EnableShortcut != Model.EnableShortcut )
            {
                Model.EnableShortcut = _modSettings.EnableShortcut;
                hasUpdate = true;
            }

            if ( _modSettings.OperationMode != Model.OperationMode.ToString( ) )
            {
                Model.OperationMode = Enum.Parse<ViewOperationMode>( _modSettings.OperationMode );
                hasUpdate = true;
            }

            if ( _modSettings.ExpertMode != Model.ExpertMode )
            {
                Model.ExpertMode = _modSettings.ExpertMode;
                hasUpdate = true;
            }

            if ( hasUpdate )
                TriggerUpdate( );
        }

        [OnTrigger]
        private void OnUpdateQuery( )
        {
            if ( Model == null || _indexer == null )
                return;

            var currentKey = _indexer.GenerateKey( Model );

            if ( currentKey == _lastQueryKey )
                return;

            _indexer.Query( Enum.TryParse<SearchSpeed>( _modSettings.SearchSpeed, out var searchSpeed ) ? searchSpeed : SearchSpeed.Medium,
                Model, ( key, result ) =>
            {
                if ( string.IsNullOrEmpty( result.Json ) )
                {
                    UnityEngine.Debug.LogWarning( "Empty JSON payload for query" );
                    return;
                }

                if ( !string.IsNullOrEmpty( Model.Search ) )
                {
                    _lastSearch = Model.Search.ToLowerInvariant( ).Trim( );
                    _lastSearchTime = UnityEngine.Time.time;
                }
                else
                    _lastSearch = null;

                _queryResults.Enqueue( (key, result.Json) );
            }, ( ) =>
            {
                GameManager.instance.userInterface.view.View.TriggerEvent( "findstuff.onShowLoader" );
            } );

            _lastQueryKey = currentKey;
        }

        private bool PickerShortcutTrigger( )
        {
            return EnableShortcut && Input.GetKey( KeyCode.LeftControl );
        }

        public void UpdatePicker( bool isPicking )
        {
            Model.IsPicking = isPicking;

            if ( Model.IsPicking && _toolSystem.activeTool != _pickerToolSystem )
            {
                _toolSystem.activeTool = _pickerToolSystem;
            }
            else if ( !Model.IsPicking && _toolSystem.activeTool == _pickerToolSystem )
            {
                _toolSystem.activeTool = _defaulToolSystem;
            }

            if ( !Model.IsPicking )
                IsPickingShortcut = false;

            TriggerUpdate( );
        }

        public void UpdatePrefabFromPicker( string prefabName )
        {
            var prefabSettings = _indexer.GetPrefab( prefabName );

            if ( prefabSettings.Prefab == null )
                return;

            Model.Selected = prefabSettings.Prefab;

            // Only set filters when not visible and in hide asset menu mode
            if ( !Model.IsVisible && Model.OperationMode == ViewOperationMode.HideAssetMenu )
            {
                Model.Filter = prefabSettings.Filter;
                Model.SubFilter = prefabSettings.SubFilter;
            }
        }

        [OnTrigger]
        private void OnNeedHighlightPrefab( string idString )
        {
            if ( string.IsNullOrEmpty( idString ) || !int.TryParse( idString, out var id ) )
                return;

            var prefabSettings = _indexer.GetPrefab( id );

            if ( prefabSettings.Prefab != null )
            {
                Model.Selected = prefabSettings.Prefab;

                // Only set filters when not visible and in hide asset menu mode
                if ( !Model.IsVisible && Model.OperationMode == ViewOperationMode.HideAssetMenu )
                {
                    Model.Filter = prefabSettings.Filter;
                    Model.SubFilter = prefabSettings.SubFilter;
                }

                TriggerUpdate( );
            }
        }

        static readonly HashSet<string> GetEvilPrefabs = ["lane editor container", "traffic spawner", "NA_DeliveryVan01", "EU_DeliveryVan01", "MotorbikeDelivery01"];
    }
}
