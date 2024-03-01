using Game.Prefabs;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class CityServiceHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType
        {
            get;
            private set;
        } = "ServiceBuilding";

        public string CategoryType => "Buildings";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            var meta = new Dictionary<string, object>();

            if ( entityManager.HasComponent<ServiceUpgradeData>( entity ) && entityManager.HasComponent<StaticObjectData>( entity ) )
            {                
                meta.Add( IBaseHelper.META_BUILDING_STATIC_UPGRADE, true );
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
            PrefabType = "ServiceBuilding";

            List<string> Tags = new List<string>( );

            if ( entityManager == null || !entityManager.Exists( entity ) )
                return new List<string>( );

            Tags.Add( "building" );
            Tags.Add( "service" );

            if ( entityManager.HasComponent<ServiceUpgradeData>( entity ) )
            {
                Tags.Add( "upgrade" );
            }

            if ( entityManager.HasComponent<FireStationData>( entity ) )
            {
                Tags.Add( "fire" );
            }
            else if ( entityManager.HasComponent<PoliceStationData>( entity ) )
            {
                Tags.Add( "police" );
            }
            else if ( entityManager.HasComponent<PrisonData>( entity ) )
            {
                Tags.Add( "prison" );
            }
            else if ( entityManager.HasComponent<HospitalData>( entity ) )
            {
                Tags.Add( "hospital" );
            }
            else if ( entityManager.HasComponent<GarbageFacilityData>( entity ) )
            {
                Tags.Add( "garbage" );
            }
            else if ( entityManager.HasComponent<PowerPlantData>( entity ) )
            {
                Tags.Add( "power-plant" );
                Tags.Add( "electricity" );
            }            
            else if ( entityManager.HasComponent<CargoTransportStationData>( entity ) )
            {
                Tags.Add( "cargo" );
            }
            else if ( entityManager.HasComponent<ParkData>( entity ) )
            {
                Tags.Add( "park" );
                PrefabType = "Park";
            }
            else if ( entityManager.HasComponent<ParkingFacilityData>( entity ) )
            {
                Tags.Add( "parking" );
                PrefabType = "Parking";
            }
            else if ( entityManager.HasComponent<AdminBuildingData>( entity ) )
            {
                Tags.Add( "administration" );
            }
            else if ( entityManager.HasChunkComponent<TransportDepotData>( entity ) )
            {
                Tags.Add( "depot" );
            }
            else if ( entityManager.HasChunkComponent<PublicTransportStationData>( entity ) )
            {
                Tags.Add( "station" );
                Tags.Add( "public-transport" );
            }
            else if ( entityManager.HasChunkComponent<MaintenanceDepotData>( entity ) )
            {
                Tags.Add( "maintenance" );
            }
            else if ( entityManager.HasChunkComponent<TelecomFacilityData>( entity ) )
            {
                Tags.Add( "telecom" );
            }
            else if ( entityManager.HasChunkComponent<ResearchFacilityData>( entity ) )
            {
                Tags.Add( "research" );
            }
            else if ( entityManager.HasChunkComponent<DeathcareFacilityData>( entity ) )
            {
                Tags.Add( "deathcare" );
            }
            else if ( entityManager.HasChunkComponent<SchoolData>( entity ) )
            {
                Tags.Add( "school" );
            }
            else if ( entityManager.HasChunkComponent<WelfareOfficeData>( entity ) )
            {
                Tags.Add( "welfare" );
            }
            else if ( entityManager.HasChunkComponent<PostFacilityData>( entity ) )
            {
                Tags.Add( "post" );
            }
            if ( entityManager.HasComponent<ElectricityConnectionData>( entity ) )
            {
                Tags.Add( "electricity-connection" );
            }
            else if ( entityManager.HasComponent<WaterSourceData>( entity ) )
            {
                Tags.Add( "water-source" );
            }
            else if ( entityManager.HasComponent<WaterPipeConnectionData>( entity ) )
            {
                Tags.Add( "water-connection" );
            }
            
            if ( entityManager.HasComponent<SewageOutlet>( entity ) )
            {
                Tags.Add( "sewage" );
            }
            Tags = Tags.OrderBy( t => t ).ToList( );

            return Tags;
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return (entityManager.HasComponent<BuildingData>( entity ) && (entityManager.HasComponent<ServiceObjectData>( entity ) || entityManager.HasComponent<ServiceUpgradeData>( entity )) ||
                entityManager.HasComponent<ServiceUpgradeData>( entity ) && entityManager.HasComponent<StaticObjectData>( entity ) );
        }

        public bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return false;
        }
    }
}
