using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class TransportStopHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType => "TransportStop";

        public string CategoryType => "Misc";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            return new Dictionary<string, object>( );
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            tags.Add( "public transport" );
            tags.Add( "stop" );

            return tags;
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return entityManager.HasComponent<TransportStopData>( entity );
        }

        public bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return false;
        }
    }
}
