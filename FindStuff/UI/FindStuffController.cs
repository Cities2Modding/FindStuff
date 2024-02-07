using Colossal.Entities;
using Colossal.Localization;
using Colossal.Serialization.Entities;
using FindStuff.Configuration;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Gooee.Plugins;
using Gooee.Plugins.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine.InputSystem;

namespace FindStuff.UI
{
    public class FindStuffController : Controller<FindStuffViewModel>
    {
        const string META_IS_DANGEROUS = "IsDangerous";
        const string META_IS_DANGEROUS_REASON = "IsDangerousReason";
        const string META_IS_SPAWNABLE = "IsSpawnable";
        const string META_ZONE_LOT_DEPTH = "ZoneLotDepth";
        const string META_ZONE_LOT_WIDTH = "ZoneLotWidth";
        const string META_ZONE_LOT_SUM = "ZoneLotSum";

        private ToolSystem _toolSystem;
        private PrefabSystem _prefabSystem;
        private ImageSystem _imageSystem;
        private ToolbarUISystem _toolbarUISystem;
        private InputAction _enableAction;

        static FieldInfo _prefabsField = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private Dictionary<string, PrefabBase> _prefabInstances = [];
        private EntityArchetype _entityArchetype;
        private EndFrameBarrier _endFrameBarrier;
        static MethodInfo _selectAsset = typeof( ToolbarUISystem ).GetMethods( BindingFlags.Instance | BindingFlags.NonPublic )
            .FirstOrDefault( m => m.Name == "SelectAsset" && m.GetParameters( ).Length == 1 );

        public readonly static FindStuffConfig _config = ConfigBase.Load<FindStuffConfig>( );

        private PrefabIndexer _indexer;
        private LocalizationManager _localizationManager;

        public override FindStuffViewModel Configure( )
        {
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>( );
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _imageSystem = World.GetOrCreateSystemManaged<ImageSystem>( );
            _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>( );
            _toolbarUISystem = World.GetOrCreateSystemManaged<ToolbarUISystem>( );
            _localizationManager = GameManager.instance.localizationManager;

            _toolSystem.EventToolChanged += ( tool =>
            {
                if ( Model.IsVisible )
                {
                    //Model.IsVisible = false;
                    //TriggerUpdate( );
                }
            } );

            _entityArchetype = this.EntityManager.CreateArchetype( ComponentType.ReadWrite<Unlock>( ), ComponentType.ReadWrite<Game.Common.Event>( ) );

            var model = new FindStuffViewModel( );

            model.Favourites = _config.Favourites.ToList( ); // Create a copy
            model.ViewMode = _config.ViewMode;
            model.Filter = _config.Filter;
            model.SubFilter = _config.SubFilter;
            model.OrderByAscending = _config.OrderByAscending;

            return model;
        }

        protected override void OnGameLoadingComplete( Purpose purpose, GameMode mode )
        {
            base.OnGameLoadingComplete( purpose, mode );

            if ( mode == GameMode.Game )
            {
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

                var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
                UnityEngine.Debug.Log( $"Getting {prefabs.Count} prefabs" );

                var prefabsList = new List<PrefabItem>( );

                foreach ( var prefabBase in prefabs )
                {
                    if ( !ProcessPrefab( prefabBase, out var prefabType, out var categoryType, out var tags, out var meta ) )
                        continue;

                    var entity = _prefabSystem.GetEntity( prefabBase );
                    var prefabIcon = "";

                    var thumbnail = _imageSystem.GetThumbnail( entity );
                    var typeIcon = GetTypeIcon( prefabType );

                    if ( thumbnail == null || thumbnail == _imageSystem.placeholderIcon )
                        prefabIcon = typeIcon;
                    else
                        prefabIcon = thumbnail;

                    var prefabItem = new PrefabItem
                    {
                        ID = entity.Index,
                        Name = prefabBase.name,
                        Type = prefabType,
                        Category = categoryType,
                        Thumbnail = prefabIcon,
                        TypeIcon = typeIcon,
                        Meta = meta,
                        Tags = tags,
                    };
                    prefabsList.Add( prefabItem );

                    if ( _prefabInstances.ContainsKey( prefabBase.name ) )
                        _prefabInstances.Remove( prefabBase.name );

                    _prefabInstances.Add( prefabBase.name, prefabBase );
                }

                _indexer = new PrefabIndexer( prefabsList );
                _indexer.Build( );
                Model.Prefabs = prefabsList;
                TriggerUpdate( );
            }
        }

        protected override void OnDestroy( )
        {
            base.OnDestroy( );
            _enableAction?.Disable( );
            _enableAction?.Dispose( );
        }

        private bool ProcessPrefab( PrefabBase prefab, out string prefabType, out string categoryType, out List<string> tags, out Dictionary<string, object> meta )
        {
            tags = new List<string>( );
            meta = new Dictionary<string, object>( );

            var prefabEntity = _prefabSystem.GetEntity( prefab );
            bool isValid = false;
            prefabType = "Unknown";
            categoryType = "None";

            if ( EntityManager.HasComponent<PlantData>( prefabEntity ) && !EntityManager.HasComponent<TreeData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "Plant";
                categoryType = "Foliage";
                tags.Add( "plant" );
            }
            else if ( EntityManager.HasComponent<TreeData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "Tree";
                categoryType = "Foliage";
                tags.Add( "tree" );
            }
            else if ( prefab is SurfacePrefab && EntityManager.HasComponent<RenderedAreaData>( prefabEntity ) && EntityManager.HasComponent<SurfaceData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "Surface";
                categoryType = "None";
                tags.Add( "surface" );
            }
            else if ( EntityManager.HasComponent<NetData>( prefabEntity ) )
            {
                // Flag invisible roads as dangerous. They can make your savegame break if not used properly.
                if ( prefab.name.ToLower( ).Contains( "invisible" ) )
                {
                    meta.Add( META_IS_DANGEROUS, true );
                    meta.Add( META_IS_DANGEROUS_REASON, "This asset could break your save game if not used properly." );
                }

                isValid = true;
                prefabType = "Network";
                categoryType = "None";
                tags.Add( "network" );
                if ( prefab.name.ToLower( ).Contains( "tram" ) )
                {
                    tags.Add( "tram" );
                }

                if ( prefab.name.ToLower( ).Contains( "train" ) )
                {
                    tags.Add( "train" );
                }

                if ( prefab.name.ToLower( ).Contains( "road" ) )
                {
                    tags.Add( "road" );
                }

                if ( prefab.name.ToLower( ).Contains( "bridge" ) )
                {
                    tags.Add( "bridge" );
                }
            }
            else if ( EntityManager.HasComponent<BuildingData>( prefabEntity ) && EntityManager.HasComponent<ServiceObjectData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "ServiceBuilding";
                categoryType = "Buildings";
                tags.Add( "building" );

                if ( EntityManager.HasComponent<FireStationData>( prefabEntity ) )
                {
                    tags.Add( "fire-department" );
                }
                else if ( EntityManager.HasComponent<PoliceStationData>( prefabEntity ) )
                {
                    tags.Add( "police" );
                }
                else if ( EntityManager.HasComponent<PrisonData>( prefabEntity ) )
                {
                    tags.Add( "prison" );
                }
                else if ( EntityManager.HasComponent<HospitalData>( prefabEntity ) )
                {
                    tags.Add( "hospital" );
                }
                else if ( EntityManager.HasComponent<GarbageFacilityData>( prefabEntity ) )
                {
                    tags.Add( "garbage" );
                }
                else if ( EntityManager.HasComponent<PowerPlantData>( prefabEntity ) )
                {
                    tags.Add( "power" );
                }
                else if ( EntityManager.HasComponent<CargoTransportStationData>( prefabEntity ) )
                {
                    tags.Add( "cargo" );
                }
                else if ( EntityManager.HasComponent<ParkData>( prefabEntity ) )
                {
                    tags.Add( "park" );
                }
                else if ( EntityManager.HasComponent<ParkingFacilityData>( prefabEntity ) )
                {
                    tags.Add( "parking" );
                }
                else if ( EntityManager.HasComponent<AdminBuildingData>( prefabEntity ) )
                {
                    tags.Add( "administration" );
                }
                else if ( EntityManager.HasChunkComponent<TransportDepotData>( prefabEntity ) )
                {
                    tags.Add( "depot" );
                }
                else if ( EntityManager.HasChunkComponent<PublicTransportStationData>( prefabEntity ) )
                {
                    tags.Add( "transport" );
                }
                else if ( EntityManager.HasChunkComponent<MaintenanceDepotData>( prefabEntity ) )
                {
                    tags.Add( "maintenance" );
                }
                else if ( EntityManager.HasChunkComponent<TelecomFacilityData>( prefabEntity ) )
                {
                    tags.Add( "telecom" );
                }
                else if ( EntityManager.HasChunkComponent<ResearchFacilityData>( prefabEntity ) )
                {
                    tags.Add( "research" );
                }
                else if ( EntityManager.HasChunkComponent<DeathcareFacilityData>( prefabEntity ) )
                {
                    tags.Add( "deathcare" );
                }
                else if ( EntityManager.HasChunkComponent<SchoolData>( prefabEntity ) )
                {
                    tags.Add( "school" );
                }
                else if ( EntityManager.HasChunkComponent<WelfareOfficeData>( prefabEntity ) )
                {
                    tags.Add( "welfare" );
                }
                else if ( EntityManager.HasChunkComponent<PostFacilityData>( prefabEntity ) )
                {
                    tags.Add( "post" );
                }
            }
            else if ( EntityManager.HasComponent<SignatureBuildingData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "SignatureBuilding";
                categoryType = "Buildings";
                tags.Add( "signature" );
                tags.Add( "building" );
            }
            else if ( EntityManager.HasComponent<VehicleData>( prefabEntity ) )
            {
                tags.Add( "vehicle" );

                if ( EntityManager.HasComponent<TrainData>( prefabEntity ) )
                {
                    meta.Add( META_IS_DANGEROUS, true );
                    meta.Add( META_IS_DANGEROUS_REASON, "This asset can't be removed with bulldozer tool after placing." );
                    tags.Add( "train" );
                }

                if ( EntityManager.HasComponent<CarData>( prefabEntity ) )
                {
                    tags.Add( "car" );
                }

                if ( EntityManager.HasComponent<DeliveryTruckData>( prefabEntity ) )
                {
                    tags.Add( "truck" );
                }

                isValid = true;
                prefabType = "Vehicle";
                categoryType = "Misc";
            }
            else if ( prefab is BuildingPrefab buildingPrefab && EntityManager.TryGetComponent( prefabEntity, out SpawnableBuildingData spawnableBuildingData ) )
            {
                if ( spawnableBuildingData.m_ZonePrefab != Entity.Null && EntityManager.TryGetComponent( spawnableBuildingData.m_ZonePrefab, out ZoneData zoneData ) )
                {
                    var areaType = zoneData.m_AreaType;
                    tags.Add( "spawnable" );
                    tags.Add( "zone" );

                    meta.Add( META_ZONE_LOT_DEPTH, buildingPrefab.m_LotDepth );
                    meta.Add( META_ZONE_LOT_WIDTH, buildingPrefab.m_LotWidth );
                    meta.Add( META_ZONE_LOT_SUM, buildingPrefab.m_LotDepth * buildingPrefab.m_LotWidth );

                    categoryType = "Zones";

                    switch ( areaType )
                    {
                        case Game.Zones.AreaType.Commercial:
                            prefabType = "ZoneCommercial";
                            break;
                        case Game.Zones.AreaType.Residential:
                            prefabType = "ZoneResidential";
                            break;
                        case Game.Zones.AreaType.Industrial:
                            ZonePrefab zonePrefab = _prefabSystem.GetPrefab<ZonePrefab>( spawnableBuildingData.m_ZonePrefab );
                            prefabType = zonePrefab.m_Office ? "ZoneOffice" : "ZoneIndustrial";
                            break;
                    }

                    isValid = true;
                }
            }
            else if ( prefab is StaticObjectPrefab staticObjectPrefab && staticObjectPrefab.m_Meshes.Length > 0 && staticObjectPrefab.isDirty == true && staticObjectPrefab.active == true && staticObjectPrefab.components.Find( p => p is SpawnableObject ) )
            {
                tags.Add( "spawnable" );
                tags.Add( "prop" );
                isValid = true;
                prefabType = "Prop";
                categoryType = "Misc";
            }

            return isValid;
        }

        private string GetTypeIcon( string type )
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
                    return "Media/Game/Icons/LotTool.svg";

                case "Prop":
                    return "fa:solid-cube";
            }

            return "";
        }

        protected override void OnUpdate( )
        {
            base.OnUpdate( );
        }

        [OnTrigger]
        private void OnTestClick( )
        {
            //TriggerUpdate( );
        }

        [OnTrigger]
        private void OnToggleVisible( )
        {
            Model.IsVisible = !Model.IsVisible;
            //_toolSystem.activeTool = World.GetOrCreateSystemManaged<ManualDuckToolSystem>( );

            //if (_toolSystem.activeTool != null && _toolSystem.activeTool is ManualDuckToolSystem duckTool )
            //{
            //    duckTool.SetActive( Model.IsVisible );
            //}
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

                    EntityCommandBuffer commandBuffer = _endFrameBarrier.CreateCommandBuffer( );
                    var unlockEntity = commandBuffer.CreateEntity( _entityArchetype );

                    // This is a way to trigger the zone prefab zoning tool for the building
                    //if ( EntityManager.TryGetComponent( entity, out SpawnableBuildingData spawnableBuildingData ) )
                    //{
                    //    if ( spawnableBuildingData.m_ZonePrefab != Entity.Null &&
                    //        EntityManager.TryGetComponent( spawnableBuildingData.m_ZonePrefab, out ZoneData zoneData ) )
                    //    {
                    //        entity = spawnableBuildingData.m_ZonePrefab;
                    //    }
                    //}
                    // This is a way to trigger the zone prefab zoning tool for the building
                    //if ( EntityManager.TryGetComponent( entity, out SpawnableBuildingData spawnableBuildingData ) )
                    //{
                    //    if ( spawnableBuildingData.m_ZonePrefab != Entity.Null &&
                    //        EntityManager.TryGetComponent( spawnableBuildingData.m_ZonePrefab, out ZoneData zoneData ) )
                    //    {
                    //        entity = spawnableBuildingData.m_ZonePrefab;
                    //    }
                    //}

                    commandBuffer.SetComponent<Unlock>( unlockEntity, new Unlock( entity ) );

                    GameManager.instance.userInterface.view.View.ExecuteScript( $"engine.trigger('toolbar.selectAsset',{{index: {entity.Index}, version: {entity.Version}}});" );
                    //_selectAsset.Invoke( _toolbarUISystem, new object[] { entity } );                    
                }
            }
        }

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
                _config.OrderByAscending != Model.OrderByAscending )
            {            
                _config.ViewMode = Model.ViewMode;
                _config.Filter = Model.Filter;
                _config.SubFilter = Model.SubFilter;
                _config.OrderByAscending = Model.OrderByAscending;
                _config.Save( );
            }
        }

        [OnTrigger]
        private void OnUpdateQuery( )
        {
            if ( Model == null || _indexer == null )
                return;

            var result = _indexer.Query( Model, out var key );

            if ( string.IsNullOrEmpty( result.Json ) )
                return;

            // Provide the results to the client
            GameManager.instance.userInterface.view.View.TriggerEvent( "findstuff.onReceiveResults", key, result.Json );
        }
    }
}
