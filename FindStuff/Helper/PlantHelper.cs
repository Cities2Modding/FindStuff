using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class PlantHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType => "Plant";

        public string CategoryType => "Foliage";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            return new Dictionary<string, object>( );
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            tags.Add( "plant" );

            return tags;
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return entityManager.HasComponent<PlantData>( entity ) && !entityManager.HasComponent<TreeData>( entity );
        }
    }
}
