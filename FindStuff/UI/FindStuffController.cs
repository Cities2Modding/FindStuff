using Game.Tools;
using Gooee.Plugins.Attributes;
using Gooee.Plugins;
using Unity.Entities;
using Game.Prefabs;
using System.Linq;
using System.Collections.Generic;
using Game.UI;
using System.Reflection;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using static Colossal.AssetPipeline.Diagnostic.Report;

namespace FindStuff.UI
{
    public class FindStuffController : Controller<FindStuffViewModel>
    {
        private ToolSystem _toolSystem;
        private PrefabSystem _prefabSystem;
        private ImageSystem _imageSystem;

        static FieldInfo _prefabsField = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private Dictionary<string, PrefabBase> _prefabInstances = [];

        public override FindStuffViewModel Configure( )
        {
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>( );
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _imageSystem = World.GetOrCreateSystemManaged<ImageSystem>( );

            _toolSystem.EventToolChanged += ( tool =>
            {
                if ( Model.IsVisible )
                {
                    Model.IsVisible = false;
                    TriggerUpdate( );
                }
            } );

            var model = new FindStuffViewModel( );

            return model;
        }

        protected override void OnGameLoadingComplete( Purpose purpose, GameMode mode )
        {
            base.OnGameLoadingComplete( purpose, mode );

            if ( mode == GameMode.Game )
            {
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
            if ( EntityManager.HasComponent<TreeData>( prefabEntity ) || EntityManager.HasComponent<PlantData>( prefabEntity ) )
            {
                return "Foliage";
            }
            else if ( EntityManager.HasComponent<NetData>( prefabEntity ) )
            {
                return "Network";
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
            else if (EntityManager.HasComponent<ResidentialProperty>(prefabEntity))
            {
                return "ZoneResidential";
            }
            else if (EntityManager.HasComponent<SpawnableBuildingData>(prefabEntity))
            {
                UnityEngine.Debug.Log("spawnable found.");
                return "ZoneIndustrial";
            }

            return "Unknown";
        }

        private string GetTypeIcon( string type )
        {
            switch ( type )
            {
                case "Foliage":
                    return "Media/Game/Icons/Forest.svg";

                case "Network":
                    return "Media/Game/Icons/Roads.svg";

                case "ServiceBuilding":
                    return "Media/Game/Icons/Services.svg";

                case "Signature":
                    return "Media/Game/Icons/ZoneSignature.svg";

                case "Zoneable":
                    return "Media/Game/Icons/Zones.svg";

                case "ZoneResidential":
                    return "Media/Game/Icons/ZoneResidential.svg";

                case "ZoneCommercial":
                    return "Media/Game/Icons/ZoneCommercial.svg";

                case "ZoneIndustrial":
                    return "Media/Game/Icons/ZoneIndustrial.svg";

                case "Vehicle":
                    return "Media/Game/Icons/Traffic.svg";
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
    }
}
