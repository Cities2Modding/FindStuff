using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class SurfaceHelper(EntityManager entityManager) : IBaseHelper
    {
        public string PrefabType => "Surface";

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

            tags.Add("surface");

            return tags;
        }

        public bool IsValidPrefab(PrefabBase prefab, Entity entity)
        {
            if (entityManager == null || prefab == null || entity == Entity.Null)
                return false;

            return prefab is SurfacePrefab && entityManager.HasComponent<RenderedAreaData>(entity) && entityManager.HasComponent<SurfaceData>(entity);
        }
    }
}
