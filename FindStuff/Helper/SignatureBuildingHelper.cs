using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    internal class SignatureBuildingHelper(EntityManager entityManager) : IBaseHelper
    {
        public string PrefabType => "SignatureBuilding";

        public string CategoryType => "Buildings";

        public Dictionary<string, object> CreateMeta(PrefabBase prefab, Entity entity)
        {
            return new Dictionary<string, object>();
        }

        public List<string> CreateTags(PrefabBase prefab, Entity entity)
        {
            List<string> tags = new List<string>();

            if (entityManager == null)
                return tags;

            tags.Add("signature");
            tags.Add("building");

            return tags;
        }

        public bool IsValidPrefab(PrefabBase prefab, Entity entity)
        {
            if (entityManager == null || entity == Entity.Null)
                return false;

            return entityManager.HasComponent<SignatureBuildingData>(entity);
        }
    }
}
