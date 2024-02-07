using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class PropHelper(EntityManager entityManager) : IBaseHelper
    {
        public string PrefabType => _PrefabType;
        private string _PrefabType = "PropMisc";

        public string CategoryType => "Props";

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

            string prefabLowered = prefab.name.ToLower();
            if (prefabLowered.StartsWith("sign"))
            {
                _PrefabType = "SignsAndPosters";
                tags.Add("sign");
            }
            else if (prefabLowered.StartsWith("poster"))
            {
                tags.Add("poster");
                _PrefabType = "SignsAndPosters";
            }
            else if (prefabLowered.StartsWith("billboard"))
            {
                tags.Add("billboard");
                _PrefabType = "Billboards";
            }
            else if (prefabLowered.StartsWith("fence"))
            {
                tags.Add("fence");
                _PrefabType = "Fences";
            }
            else
            {
                _PrefabType = "PropMisc";
            }

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
