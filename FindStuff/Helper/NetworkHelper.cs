using Game.Prefabs;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class NetworkHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType
        {
            get;
            private set;
        } = "OtherNetwork";

        public string CategoryType => "Network";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            Dictionary<string, object> meta = new Dictionary<string, object>( );

            if ( prefab.name.ToLower( ).Contains( "invisible " ) )
            {
                meta.Add( IBaseHelper.META_IS_DANGEROUS, true );
                meta.Add( IBaseHelper.META_IS_DANGEROUS_REASON, "FindStuff.Dangerous.CorruptWarning" );
            }

            var placeableObject = prefab.GetComponent<PlaceableObject>( );

            if ( placeableObject != null )
            {
                meta.Add( "Cost", placeableObject.m_ConstructionCost );
                meta.Add( "XPReward", placeableObject.m_XPReward );
            }

            return meta;
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            PrefabType = "OtherNetwork";

            tags.Add( "network" );

            if ( prefab.name.ToLower( ).Contains( "bridge" ) )
            {
                tags.Add( "bridge" );
            }
            
            if ( prefab.TryGet<UIObject>( out var uiObject ))
            {
                switch ( uiObject.m_Group.name )
                {
                    case "RoadsSmallRoads":
                        tags.Add( "small-road" );
                        tags.Add( "road" );
                        PrefabType = "SmallRoad";
                        break;

                    case "RoadsMediumRoads":
                        tags.Add( "medium-road" );
                        tags.Add( "road" );
                        PrefabType = "MediumRoad";
                        break;

                    case "RoadsLargeRoads":
                        tags.Add( "large-road" );
                        tags.Add( "road" );
                        PrefabType = "LargeRoad";
                        break;

                    case "RoadsHighways":
                        tags.Add( "highway" );
                        tags.Add( "road" );
                        PrefabType = "Highway";
                        break;

                    case "RoadsServices":
                        tags.Add( "road-tool" );
                        PrefabType = "RoadTool";
                        break;

                    case "RoadsRoundabouts":
                        tags.Add( "roundabout" );
                        tags.Add( "road" );
                        PrefabType = "Roundabout";
                        break;

                    case "TransportationWater":
                        tags.Add( "waterway" );
                        break;

                    case "Pathways":
                        tags.Add( "pavement" );
                        PrefabType = "Pavement";
                        break;

                    case "TransportationRoad":
                        tags.Add( "public-transport" );
                        tags.Add( "road" );
                        break;

                    case "TransportationTrain":
                        tags.Add( "train" );
                        break;

                    case "TransportationTram":
                        tags.Add( "tram" );
                        break;

                    case "TransportationSubway":
                        tags.Add( "subway" );
                        break;

                    default:
                        tags.Add( uiObject.m_Group.name );
                        break;
                }
            }

            if ( entityManager.HasComponent<MarkerNetData>( entity ) )
            {
                tags.Add( "marker" );
            }

            if ( entityManager.HasComponent<ElectricityConnectionData>( entity ) )
            {
                tags.Add( "electricity-connection" );
            }

            if ( entityManager.HasComponent<PipelineData>( entity ) && !entityManager.HasComponent<MarkerNetData>( entity ) )
            {
                tags.Add( "pipe" );
            }
            else if ( entityManager.HasComponent<PowerLineData>( entity ) )
            {
                tags.Add( "power-line" );
            }

            return tags.OrderBy( t => t ).ToList( );
        }

        public bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return prefab.name.ToLower( ).Contains( "invisible" );
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return entityManager.HasComponent<NetData>( entity ) || prefab is StaticObjectPrefab staticObject && staticObject.components.Find( p => p is NetObject );
        }
    }
}
