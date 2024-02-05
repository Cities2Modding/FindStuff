using Game.Tools;
using Gooee.Plugins.Attributes;
using Gooee.Plugins;
using Unity.Entities;
using Game.Prefabs;
using System.Linq;
using System.Collections.Generic;
using Game.UI;
using System.Reflection;
using Colossal.Serialization.Entities;
using Game;
using Game.Companies;
using Colossal.Entities;
using Game.UI.InGame;
using UnityEngine.InputSystem;

namespace FindStuff.UI
{
    public class FindStuffController : Controller<FindStuffViewModel>
    {
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
                    _enableAction.Disable();
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
                foreach ( var prefabBase in prefabs.Where( p => IsValid( _prefabSystem.GetEntity( p ) ) ) )
                {
                    var entity = _prefabSystem.GetEntity( prefabBase );

                    var type = GetType( prefabBase, entity );
                    var isSpawnableBuilding = EntityManager.HasComponent<SpawnableBuildingData>( entity );
                    var prefabIcon = "";

                    var thumbnail = _imageSystem.GetThumbnail( entity );
                    var typeIcon = GetTypeIcon( type );

                    if ( thumbnail == null || thumbnail == "Media/Placeholder.svg" )
                        prefabIcon = typeIcon;
                    else
                        prefabIcon = thumbnail;

                    var prefabItem = new PrefabItem
                    {
                        Name = prefabBase.name,
                        Type = type,
                        Thumbnail = prefabIcon,
                        TypeIcon = typeIcon
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

        private bool IsValid( Entity prefabEntity )
        {
            if ( EntityManager.HasComponent<TreeData>( prefabEntity ) || 
                EntityManager.HasComponent<PlantData>( prefabEntity ) )
            {
                return true;
            }
            else if (EntityManager.HasComponent<NetData>( prefabEntity ) )
            {
                return true;
            }
            else if (EntityManager.HasComponent<BuildingData>(prefabEntity) && EntityManager.HasComponent<ServiceObjectData>(prefabEntity))
            {
                return true;
            }
            else if (EntityManager.HasComponent<VehicleData>( prefabEntity ) )
            {
                return true;
            }
            else if (EntityManager.HasComponent<ServiceCompanyData>(prefabEntity) && EntityManager.HasComponent<CommercialCompanyData>(prefabEntity))
            {
                return true;
            }
            else if ( EntityManager.HasComponent<SpawnableBuildingData>(prefabEntity) )
            {
                return true;
            }

            return false;
        }

        private string GetType( PrefabBase prefab, Entity prefabEntity )
        {
            if ( EntityManager.HasComponent<PlantData>( prefabEntity ) )
            {
                return "Plant";
            }
            else if( EntityManager.HasComponent<TreeData>( prefabEntity ) )
            {
                return "Tree";
            }             
            else if ( EntityManager.HasComponent<NetData>( prefabEntity ) )
            {
                return "Network";
            }
            else if ( EntityManager.HasComponent<SurfaceData>( prefabEntity ) )
            {
                return "Surface";
            }
            else if (EntityManager.HasComponent<BuildingData>(prefabEntity) && EntityManager.HasComponent<ServiceObjectData>(prefabEntity))
            {
                return "ServiceBuilding";
            }
            else if ( EntityManager.HasComponent<SignatureBuildingData>( prefabEntity ) )
            {
                return "SignatureBuilding";
            }
            else if ( EntityManager.HasComponent<VehicleData>( prefabEntity ) )
            {
                return "Vehicle";
            }
            else if (EntityManager.TryGetComponent<SpawnableBuildingData>(prefabEntity, out var spawnableBuildingData))
            {
                if (spawnableBuildingData.m_ZonePrefab != Entity.Null && EntityManager.TryGetComponent<ZoneData>(spawnableBuildingData.m_ZonePrefab, out var zoneData))
                {
                    var areaType = zoneData.m_AreaType;
                    switch (areaType)
                    {
                        case Game.Zones.AreaType.Commercial:
                            return "ZoneCommercial";

                        case Game.Zones.AreaType.Residential:
                            return "ZoneResidential";

                        case Game.Zones.AreaType.Industrial:
                            ZonePrefab zonePrefab = _prefabSystem.GetPrefab<ZonePrefab>(spawnableBuildingData.m_ZonePrefab);
                            return zonePrefab.m_Office ? "ZoneOffice" : "ZoneIndustrial";

                        default:
                            return "Unknown";
                    }
                }
            }

            return "Unknown";
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

        private bool IsOffice(IndustrialProcessData industrialProcessData)
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
