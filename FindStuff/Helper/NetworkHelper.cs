using Colossal.Entities;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            if ( entityManager.TryGetComponent<PlaceableNetData>( entity, out var placeableNetData ) )
            {
                meta.Add( "Cost", Convert.ToInt32( placeableNetData.m_DefaultConstructionCost ) * 125 );
                meta.Add( "XPReward", placeableNetData.m_XPReward );
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

            //var prefabSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>( );

            //if ( entityManager.TryGetComponent<RoadData>( entity, out var roadData ) )
            //{
            //    tags.Add( $"{roadData.m_SpeedLimit} km/h" );
            //}

            //if ( prefab is NetPrefab && prefab is NetGeometryPrefab geometryPrefab )
            //{
            //    if ( geometryPrefab.m_Sections?.Length > 0 )
            //    {
            //        foreach ( var section in geometryPrefab.m_Sections )
            //        {
            //            if ( section?.m_Section?.m_Pieces?.Length > 0 )
            //            {
            //                foreach ( var sectionPiece in section.m_Section.m_Pieces )
            //                {
            //                    var piecePrefab = sectionPiece.m_Piece;

            //                    if ( piecePrefab == null )
            //                        continue;
                                
            //                    var lanes = piecePrefab.GetComponent<NetPieceLanes>( );
            //                    if ( lanes?.m_Lanes?.Length > 0 )
            //                    {
            //                        var isValid = false;

            //                        var lanePrefabs = lanes.m_Lanes.Select( l => l.m_Lane );

            //                        foreach ( var lanePrefab in lanePrefabs )
            //                        {
            //                            var laneEntity = prefabSystem.GetEntity( lanePrefab );
            //                            var netLaneData = entityManager.GetComponentData<NetLaneData>( laneEntity );

            //                            if ( !entityManager.TryGetComponent<CarLaneData>( laneEntity, out var carLaneData ) )
            //                                continue;

            //                            var carLanePrefab = lanePrefab.GetComponent<CarLane>( );

            //                            if ( carLanePrefab?.m_RoadType != Game.Net.RoadTypes.Car )
            //                                continue;

            //                            isValid = true;
            //                        }
                                    
            //                        if ( isValid )
            //                        {
            //                            tags.Add( $"{piecePrefab.name} {lanes.m_Lanes.Length} - {piecePrefab.m_Layer}" );
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            if ( prefab.name.ToLower( ).Contains( "bridge" ) )
            {
                tags.Add( "bridge" );
            }
            
            if ( prefab.TryGet<UIObject>( out var uiObject ) && uiObject.m_Group != null)
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
                        tags.Add(uiObject.m_Group.name);
                        break;
                }
            }

            if ( entityManager.HasComponent<MarkerNetData>( entity ) || prefab is MarkerObjectPrefab)
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

            // Adds integrated bus, train, tramm, ect. stops
            if ( prefab is MarkerObjectPrefab && prefab.name.Contains("Integrated"))
                return true;

            if (entityManager.HasComponent<NetData>(entity))
                return true;

            return prefab is StaticObjectPrefab staticObject && staticObject.components.Find( p => p is NetObject );
        }
    }
}
