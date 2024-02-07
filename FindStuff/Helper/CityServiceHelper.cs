using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class CityServiceHelper(EntityManager entityManager) : IBaseHelper
    {
        public string PrefabType => "ServiceBuilding";

        public string CategoryType => "Buildings";

        public Dictionary<string, object> CreateMeta(PrefabBase prefab, Entity entity)
        {
            return new Dictionary<string, object>();
        }

        public List<string> CreateTags(PrefabBase prefab, Entity entity)
        {
            List<string> Tags = new List<string>();

            if (entityManager == null || !entityManager.Exists(entity))
                return new List<string>();

            Tags.Add("building");

            if (entityManager.HasComponent<FireStationData>(entity))
            {
                Tags.Add("fire-department");
            }
            else if (entityManager.HasComponent<PoliceStationData>(entity))
            {
                Tags.Add("police");
            }
            else if (entityManager.HasComponent<PrisonData>(entity))
            {
                Tags.Add("prison");
            }
            else if (entityManager.HasComponent<HospitalData>(entity))
            {
                Tags.Add("hospital");
            }
            else if (entityManager.HasComponent<GarbageFacilityData>(entity))
            {
                Tags.Add("garbage");
            }
            else if (entityManager.HasComponent<PowerPlantData>(entity))
            {
                Tags.Add("power");
            }
            else if (entityManager.HasComponent<CargoTransportStationData>(entity))
            {
                Tags.Add("cargo");
            }
            else if (entityManager.HasComponent<ParkData>(entity))
            {
                Tags.Add("park");
            }
            else if (entityManager.HasComponent<ParkingFacilityData>(entity))
            {
                Tags.Add("parking");
            }
            else if (entityManager.HasComponent<AdminBuildingData>(entity))
            {
                Tags.Add("administration");
            }
            else if (entityManager.HasChunkComponent<TransportDepotData>(entity))
            {
                Tags.Add("depot");
            }
            else if (entityManager.HasChunkComponent<PublicTransportStationData>(entity))
            {
                Tags.Add("transport");
            }
            else if (entityManager.HasChunkComponent<MaintenanceDepotData>(entity))
            {
                Tags.Add("maintenance");
            }
            else if (entityManager.HasChunkComponent<TelecomFacilityData>(entity))
            {
                Tags.Add("telecom");
            }
            else if (entityManager.HasChunkComponent<ResearchFacilityData>(entity))
            {
                Tags.Add("research");
            }
            else if (entityManager.HasChunkComponent<DeathcareFacilityData>(entity))
            {
                Tags.Add("deathcare");
            }
            else if (entityManager.HasChunkComponent<SchoolData>(entity))
            {
                Tags.Add("school");
            }
            else if (entityManager.HasChunkComponent<WelfareOfficeData>(entity))
            {
                Tags.Add("welfare");
            }
            else if (entityManager.HasChunkComponent<PostFacilityData>(entity))
            {
                Tags.Add("post");
            }

            return Tags;
        }

        public bool IsValidPrefab(PrefabBase prefab, Entity entity)
        {
            if (entityManager == null || entity == Entity.Null)
                return false;

            return entityManager.HasComponent<BuildingData>(entity) && entityManager.HasComponent<ServiceObjectData>(entity);
        }
    }
}
