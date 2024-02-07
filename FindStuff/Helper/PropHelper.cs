using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class PropHelper(EntityManager entityManager) : IBaseHelper
    {
        public string PrefabType => "Prop";

        public string CategoryType => "Misc";

        public Dictionary<string, object> CreateMeta(PrefabBase prefab, Entity entity)
        {
            return new Dictionary<string, object>();
        }

        public List<string> CreateTags(PrefabBase prefab, Entity entity)
        {
            List<string> tags = new List<string>();

            if (entityManager == null)
                return tags;

            tags.Add("spawnable");
            tags.Add("prop");

            return tags;
        }

        public bool IsValidPrefab(PrefabBase prefab, Entity entity)
        {
            if (entityManager == null || entity == Entity.Null)
                return false;

            return prefab is StaticObjectPrefab staticObjectPrefab && staticObjectPrefab.m_Meshes.Length > 0 && staticObjectPrefab.isDirty == true && staticObjectPrefab.active == true && staticObjectPrefab.components.Find(p => p is SpawnableObject);
        }
    }
}
