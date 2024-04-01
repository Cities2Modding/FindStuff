using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class AreaHelper( EntityManager entityManager ) : BaseHelper
    {
        public override string PrefabType => "Area";

        public override string CategoryType => "Misc";

        public override Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            return new Dictionary<string, object>( );
        }

        public override List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            tags.Add( "area" );

            return tags;
        }

        public override bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || prefab == null || entity == Entity.Null )
                return false;

            return prefab is AreaPrefab && prefab is not SurfacePrefab &&
                !prefab.name.ToLowerInvariant( ).EndsWith( "_placeholder" ) &&
                !prefab.name.ToLowerInvariant( ).EndsWith( " placeholder" );
        }

        public override bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return false;
        }
    }
}
