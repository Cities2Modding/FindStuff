using Colossal.Entities;
using Game.Prefabs;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class VehicleHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType => "Vehicle";

        public string CategoryType => "Props";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            Dictionary<string, object> meta = new Dictionary<string, object>( );
            if ( entityManager.HasComponent<TrainData>( entity ) )
            {
                meta.Add( IBaseHelper.META_IS_DANGEROUS, true );
                meta.Add( IBaseHelper.META_IS_DANGEROUS_REASON, "FindStuff.Dangerous.NoDelete" );
            }

            return meta;
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;
            
            tags.Add( "prop" );
            
            // The order of execution here matters as a Bus is also Car etc.
            if ( entityManager.TryGetComponent<PublicTransportVehicleData>( entity, out var publicTransport ) )
            {
                var transportType = publicTransport.m_TransportType;
                var type = transportType.ToString( ).ToLower( );

                if ( !tags.Contains( type ) )
                    tags.Add( type );

                switch ( transportType )
                {
                    case TransportType.Airplane:
                    case TransportType.Helicopter:
                    case TransportType.Rocket:
                        tags.Add( "aircraft" );
                        break;

                    case TransportType.Ship:
                        tags.Add( "watercraft" );
                        break;

                    case TransportType.Post:
                        tags.Add( "van" );
                        break;

                    case TransportType.Taxi:
                        tags.Add( "car" );
                        break;

                    case TransportType.Subway:
                        tags.Add( "train" );
                        break;

                }

                if ( entityManager.HasComponent<CarData>( entity ) )
                    tags.Add( "automobile" );
            }
            else if ( entityManager.HasComponent<FireEngineData>( entity ) )
            {
                tags.Add( "truck" );
                tags.Add( "fire" );
                tags.Add( "emergency" );
                tags.Add( "automobile" );
            }
            else if ( entityManager.HasComponent<PoliceCarData>( entity ) )
            {
                tags.Add( "car" );
                tags.Add( "police" );
                tags.Add( "emergency" );
                tags.Add( "automobile" );
            }
            else if ( entityManager.HasComponent<AmbulanceData>( entity ) )
            {
                tags.Add( "van" );
                tags.Add( "ambulance" );
                tags.Add( "emergency" );
                tags.Add( "automobile" );
            }
            else if ( entityManager.HasComponent<CarData>( entity ) )
            {
                tags.Add( "car" );
                tags.Add( "automobile" );
            }
            else if( entityManager.HasComponent<DeliveryTruckData>( entity ) )
            {
                tags.Add( "truck" );
                tags.Add( "automobile" );
            }
            else if( entityManager.HasComponent<TrainCarriageData>( entity ) )
            {
                tags.Add( "train" );
                tags.Add( "carriage" );
            }
            else if ( entityManager.HasComponent<TrainEngineData>( entity ) )
            {
                tags.Add( "train" );
                tags.Add( "engine" );
            }
            else if ( entityManager.HasComponent<TrainData>( entity ) )
            {
                tags.Add( "train" );
            }
            else if ( entityManager.HasComponent<TaxiData>( entity ) )
            {
                tags.Add( "taxi" );
                tags.Add( "car" );
                tags.Add( "automobile" );
            }
            else if ( entityManager.HasComponent<HelicopterData>( entity ) )
            {
                tags.Add( "helicopter" );
                tags.Add( "aircraft" );
            }
            else if ( entityManager.HasComponent<AirplaneData>( entity ) )
            {
                tags.Add( "airplane" );
                tags.Add( "aircraft" );
            }
            else if ( entityManager.HasComponent<WatercraftData>( entity ) )
            {
                tags.Add( "watercraft" );
            }
            else if ( entityManager.HasComponent<CarData>( entity ) )
            {
                tags.Add( "car" );
                tags.Add( "automobile" );
            }

            return tags.OrderBy( t => t ).ToList( );
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return entityManager.HasComponent<VehicleData>( entity );
        }
    }
}
