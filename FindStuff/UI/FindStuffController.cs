using Game.Tools;
using Gooee.Plugins.Attributes;
using Gooee.Plugins;
using System;
using Unity.Entities;
using Game.Prefabs;
using System.Linq;
using System.Collections.Generic;
using Game.UI;
using System.Reflection;
using Newtonsoft.Json;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;

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

            var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
            UnityEngine.Debug.Log( "Getting prefabs" );
            var prefabsList = new List<PrefabItem>( );

            foreach ( var prefabBase in prefabs.Where( p => IsValid( _prefabSystem.GetEntity( p ) ) ) )
            {
                var entity = _prefabSystem.GetEntity( prefabBase );

                //var objectPrefab = _prefabSystem.GetPrefab<ObjectPrefab>( prefabEntity );

                //if ( objectPrefab == null )
                //    continue;
                var type = GetType( prefabBase, entity );
                var isSpawnableBuilding = EntityManager.HasComponent<SpawnableBuildingData>( entity );
                var prefabIcon = "";

                var thumbnail = _imageSystem.GetThumbnail( entity );
                var typeIcon = GetTypeIcon( type );

                if ( thumbnail == null || thumbnail == "Media/Placeholder.svg" )
                    prefabIcon = typeIcon;
                else
                    prefabIcon = thumbnail;

                var prefabItem = new PrefabItem {
                    Name = prefabBase.name,
                    Type = type,
                    Thumbnail = prefabIcon, 
                    TypeIcon = typeIcon 
                };
                prefabsList.Add( prefabItem );
                //UnityEngine.Debug.Log( "Got? prefab: " + JsonConvert.SerializeObject( prefabItem ) );
                // UnityEngine.Debug.Log( "Got prefab: " + prefabBase.name  + " icon: "+ icon );
                _prefabInstances.Add( prefabBase.name, prefabBase );
            }

            model.Prefabs = prefabsList;

            return model;
        }

        private bool IsValid( Entity prefabEntity )
        {
            if ( EntityManager.HasComponent<TreeData>( prefabEntity ) || 
                EntityManager.HasComponent<PlantData>( prefabEntity ) )
            {
                return true;
            }
            else if ( EntityManager.HasComponent<NetPieceData>( prefabEntity ) )
            {
                return true;
            }
            else if ( EntityManager.HasComponent<SignatureBuildingData>( prefabEntity ) )
            {
                return true;
            }
            else if ( EntityManager.HasComponent<SpawnableBuildingData>( prefabEntity ) )
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
            else if ( EntityManager.HasComponent<RoadData>( prefabEntity ) )
            {
                return "Network";
            }
            else if ( EntityManager.HasComponent<SignatureBuildingData>( prefabEntity ) )
            {
                return "Signature Building";
            }
            else if ( EntityManager.HasComponent<VehicleData>( prefabEntity ) )
            {
                return "Vehicle";
            }
            else if ( EntityManager.TryGetComponent<SpawnableBuildingData>( prefabEntity, out var buildingData ) )
            {
                // Not working
                if ( buildingData.m_ZonePrefab != Entity.Null &&
                    EntityManager.TryGetComponent<ZoneData>( buildingData.m_ZonePrefab, out var zd ) )
                {
                    var areaType = zd.m_AreaType;

                    switch ( areaType )
                    {
                        case Game.Zones.AreaType.Commercial:
                            return "ZoneCommercial";

                        case Game.Zones.AreaType.Residential:
                            return "ZoneResidential";

                        case Game.Zones.AreaType.Industrial:
                            return "ZoneIndustrial";
                    }
                }
            }

            return "Unknown";
        }

        private string GetTypeIcon( string type )
        {
            switch ( type )
            {
                case "Foliage":
                    return "Media/Game/Icons/Forest.svg";

                case "Roads":
                    return "Media/Game/Icons/Roads.svg";

                case "Building":
                    return "Media/Game/Icons/Roads.svg";

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
