using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class VehicleHelper(EntityManager entityManager) : IBaseHelper
    {
        public string PrefabType => "Vehicle";

        public string CategoryType => "Misc";

        public Dictionary<string, object> CreateMeta(PrefabBase prefab, Entity entity)
        {
            Dictionary<string, object> meta = new Dictionary<string, object>();
            if (entityManager.HasComponent<TrainData>(entity))
            {
                meta.Add(IBaseHelper.META_IS_DANGEROUS, true);
                meta.Add(IBaseHelper.META_IS_DANGEROUS_REASON, "FindStuff.Dangerous.NoDelete");
            }

            return meta;
        }

        public List<string> CreateTags(PrefabBase prefab, Entity entity)
        {
            List<string> tags = new List<string>();

            if (entityManager == null)
                return new List<string>();

            if (entityManager.HasComponent<TrainData>(entity))
            {
                tags.Add("train");
            }

            if (entityManager.HasComponent<CarData>(entity))
            {
                tags.Add("car");
            }

            if (entityManager.HasComponent<DeliveryTruckData>(entity))
            {
                tags.Add("truck");
            }

            tags.Add("signature");
            tags.Add("building");

            return tags;
        }

        public bool IsValidPrefab(PrefabBase prefab, Entity entity)
        {
            if (entityManager == null || entity == Entity.Null)
                return false;

            return entityManager.HasComponent<VehicleData>(entity);
        }
    }
}
