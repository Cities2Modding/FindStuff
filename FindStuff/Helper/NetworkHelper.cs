using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class NetworkHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType => "Network";

        public string CategoryType => "None";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            Dictionary<string, object> meta = new Dictionary<string, object>( );

            if ( prefab.name.ToLower( ).Contains( "invisible" ) )
            {
                meta.Add( IBaseHelper.META_IS_DANGEROUS, true );
                meta.Add( IBaseHelper.META_IS_DANGEROUS_REASON, "FindStuff.Dangerous.CorruptWarning" );
            }

            return meta;
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            tags.Add( "network" );
            if ( prefab.name.ToLower( ).Contains( "tram" ) )
            {
                tags.Add( "tram" );
            }

            if ( prefab.name.ToLower( ).Contains( "train" ) )
            {
                tags.Add( "train" );
            }

            if ( prefab.name.ToLower( ).Contains( "road" ) )
            {
                tags.Add( "road" );
            }

            if ( prefab.name.ToLower( ).Contains( "bridge" ) )
            {
                tags.Add( "bridge" );
            }

            return tags;
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return entityManager.HasComponent<NetData>( entity );
        }
    }
}
