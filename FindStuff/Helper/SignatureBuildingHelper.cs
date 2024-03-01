using Colossal.Entities;
using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    internal class SignatureBuildingHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType => "SignatureBuilding";

        public string CategoryType => "Buildings";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            var meta = new Dictionary<string, object>( );

            var placeableObject = prefab.GetComponent<PlaceableObject>( );

            if ( placeableObject != null )
            {
                meta.Add( "Cost", placeableObject.m_ConstructionCost );
                meta.Add( "XPReward", placeableObject.m_XPReward );
            }

            return meta;
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            tags.Add( "signature" );
            tags.Add( "building" );

            return tags;
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return entityManager.HasComponent<SignatureBuildingData>( entity );
        }

        public bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return false;
        }
    }
}
