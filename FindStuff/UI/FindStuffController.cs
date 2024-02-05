using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Gooee.Plugins;
using Gooee.Plugins.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

        private ToolSystem _toolSystem;
        private PrefabSystem _prefabSystem;
        private ImageSystem _imageSystem;
        private ToolbarUISystem _toolbarUISystem;
        private InputAction _enableAction;

        static FieldInfo _prefabsField = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private Dictionary<string, PrefabBase> _prefabInstances = [];
        private EntityArchetype _entityArchetype;
        private EndFrameBarrier _endFrameBarrier;
        static MethodInfo _selectTool = typeof( ToolbarUISystem ).GetMethods( BindingFlags.Instance | BindingFlags.NonPublic )
            .FirstOrDefault( m => m.Name == "SelectAsset" && m.GetParameters( ).Length == 1 );

        private bool _isValidPrefab = false;
        private Dictionary<string, object> _meta = new Dictionary<string, object>( );
        private List<string> _tags = new List<string>( );

        public override FindStuffViewModel Configure( )
        {
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>( );
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _imageSystem = World.GetOrCreateSystemManaged<ImageSystem>( );
            _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>( );
            _toolbarUISystem = World.GetOrCreateSystemManaged<ToolbarUISystem>( );

            _toolSystem.EventToolChanged += ( tool =>
            {
                if ( Model.IsVisible )
                {
                    Model.IsVisible = false;
                    TriggerUpdate( );
                }
            } );

            _entityArchetype = this.EntityManager.CreateArchetype( ComponentType.ReadWrite<Unlock>( ), ComponentType.ReadWrite<Game.Common.Event>( ) );

            var model = new FindStuffViewModel( );

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
                UnityEngine.Debug.Log( "Getting prefabs" );

                var prefabsList = new List<PrefabItem>( );

                foreach ( var prefabBase in prefabs )
                {
                    if ( !ProcessPrefab( prefabBase, out var prefabType ) )
                        continue;

                    _tags.Clear( );
                    _meta.Clear( );

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
                        Name = prefabBase.name,
                        Type = prefabType,
                        Thumbnail = prefabIcon,
                        TypeIcon = typeIcon,
                        Meta = _meta,
                        Tags = _tags,
                    };
                    prefabsList.Add( prefabItem );

                    if ( _prefabInstances.ContainsKey( prefabBase.name ) )
                        _prefabInstances.Remove( prefabBase.name );

                    _prefabInstances.Add( prefabBase.name, prefabBase );
                }

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

        private bool ProcessPrefab( PrefabBase prefab, out string prefabType )
        {
            var prefabEntity = _prefabSystem.GetEntity( prefab );
            bool isValid = false;
            prefabType = "Unknown";

            if ( EntityManager.HasComponent<PlantData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "Plant";
                _tags.Add( "plant" );
            }
            else if ( EntityManager.HasComponent<TreeData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "Tree";
                _tags.Add( "tree" );
            }
            else if ( prefab is SurfacePrefab && EntityManager.HasComponent<RenderedAreaData>( prefabEntity ) && EntityManager.HasComponent<SurfaceData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "Surface";
                _tags.Add( "surface" );
            }
            else if ( EntityManager.HasComponent<NetData>( prefabEntity ) )
            {
                // Flag invisible roads as dangerous. They can make your savegame break if not used properly.
                if ( prefab.name.ToLower( ).Contains( "invisible" ) )
                {
                    _meta.Add( META_IS_DANGEROUS, true );
                    _meta.Add( META_IS_DANGEROUS_REASON, "This asset could break your save game if not used properly." );
                }

                isValid = true;
                prefabType = "Network";
                _tags.Add( "network" );
                if ( prefab.name.ToLower( ).Contains( "tram" ) )
                {
                    _tags.Add( "tram" );
                }

                if ( prefab.name.ToLower( ).Contains( "train" ) )
                {
                    _tags.Add( "train" );
                }

                if ( prefab.name.ToLower( ).Contains( "road" ) )
                {
                    _tags.Add( "road" );
                }

                if ( prefab.name.ToLower( ).Contains( "bridge" ) )
                {
                    _tags.Add( "bridge" );
                }
            }
            else if ( EntityManager.HasComponent<BuildingData>( prefabEntity ) && EntityManager.HasComponent<ServiceObjectData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "ServiceBuilding";
                _tags.Add( "building" );

                if ( EntityManager.HasComponent<FireStationData>( prefabEntity ) )
                {
                    _tags.Add( "fire-department" );
                }
                else if ( EntityManager.HasComponent<PoliceStationData>( prefabEntity ) )
                {
                    _tags.Add( "police" );
                }
                else if ( EntityManager.HasComponent<PrisonData>( prefabEntity ) )
                {
                    _tags.Add( "prison" );
                }
                else if ( EntityManager.HasComponent<HospitalData>( prefabEntity ) )
                {
                    _tags.Add( "hospital" );
                }
                else if ( EntityManager.HasComponent<GarbageFacilityData>( prefabEntity ) )
                {
                    _tags.Add( "garbage" );
                }
                else if ( EntityManager.HasComponent<PowerPlantData>( prefabEntity ) )
                {
                    _tags.Add( "power" );
                }
                else if ( EntityManager.HasComponent<CargoTransportStationData>( prefabEntity ) )
                {
                    _tags.Add( "cargo" );
                }
                else if ( EntityManager.HasComponent<ParkData>( prefabEntity ) )
                {
                    _tags.Add( "park" );
                }
                else if ( EntityManager.HasComponent<ParkingFacilityData>( prefabEntity ) )
                {
                    _tags.Add( "parking" );
                }
                else if ( EntityManager.HasComponent<AdminBuildingData>( prefabEntity ) )
                {
                    _tags.Add( "administration" );
                }
                else if ( EntityManager.HasChunkComponent<TransportDepotData>( prefabEntity ) )
                {
                    _tags.Add( "depot" );
                }
                else if ( EntityManager.HasChunkComponent<PublicTransportStationData>( prefabEntity ) )
                {
                    _tags.Add( "transport" );
                }
                else if ( EntityManager.HasChunkComponent<MaintenanceDepotData>( prefabEntity ) )
                {
                    _tags.Add( "maintenance" );
                }
                else if ( EntityManager.HasChunkComponent<TelecomFacilityData>( prefabEntity ) )
                {
                    _tags.Add( "telecom" );
                }
                else if ( EntityManager.HasChunkComponent<ResearchFacilityData>( prefabEntity ) )
                {
                    _tags.Add( "research" );
                }
                else if ( EntityManager.HasChunkComponent<DeathcareFacilityData>( prefabEntity ) )
                {
                    _tags.Add( "deathcare" );
                }
                else if ( EntityManager.HasChunkComponent<SchoolData>( prefabEntity ) )
                {
                    _tags.Add( "school" );
                }
                else if ( EntityManager.HasChunkComponent<WelfareOfficeData>( prefabEntity ) )
                {
                    _tags.Add( "welfare" );
                }
                else if ( EntityManager.HasChunkComponent<PostFacilityData>( prefabEntity ) )
                {
                    _tags.Add( "post" );
                }
            }
            else if ( EntityManager.HasComponent<SignatureBuildingData>( prefabEntity ) )
            {
                isValid = true;
                prefabType = "SignatureBuilding";
                _tags.Add( "signature" );
                _tags.Add( "building" );
            }
            else if ( EntityManager.HasComponent<VehicleData>( prefabEntity ) )
            {
                _tags.Add( "vehicle" );

                if ( EntityManager.HasComponent<TrainData>( prefabEntity ) )
                {
                    _meta.Add( META_IS_DANGEROUS, true );
                    _meta.Add( META_IS_DANGEROUS_REASON, "This asset can't be removed with bulldozer tool after placing." );
                    _tags.Add( "train" );
                }

                if ( EntityManager.HasComponent<CarData>( prefabEntity ) )
                {
                    _tags.Add( "car" );
                }

                if ( EntityManager.HasComponent<DeliveryTruckData>( prefabEntity ) )
                {
                    _tags.Add( "truck" );
                }

                isValid = true;
                prefabType = "Vehicle";
            }
            else if ( EntityManager.TryGetComponent( prefabEntity, out SpawnableBuildingData spawnableBuildingData ) )
            {
                if ( spawnableBuildingData.m_ZonePrefab != Entity.Null && EntityManager.TryGetComponent( spawnableBuildingData.m_ZonePrefab, out ZoneData zoneData ) )
                {
                    var areaType = zoneData.m_AreaType;
                    _tags.Add( "spawnable" );
                    _tags.Add( "zone" );
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
            else
            {
                prefabType = "Unknown";
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
                    return "fa:solid-pencil";
            }

            return "";
        }

        private bool IsOffice( IndustrialProcessData industrialProcessData )
        {
            return industrialProcessData.m_Output.m_Resource switch
            {
                Game.Economy.Resource.Software or Game.Economy.Resource.Financial or Game.Economy.Resource.Media => true,
                _ => false,
            };
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
                    EntityCommandBuffer commandBuffer = _endFrameBarrier.CreateCommandBuffer( );

                    var unlockEntity = commandBuffer.CreateEntity( _entityArchetype );
                    commandBuffer.SetComponent<Unlock>( unlockEntity, new Unlock( entity ) );
                    _selectTool.Invoke( _toolbarUISystem, new object[] { entity } );
                }
            }
        }
    }
}
