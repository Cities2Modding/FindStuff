﻿using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;
using static Game.Prefabs.CharacterGroup;

namespace FindStuff.Helper
{
    public class PlantHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType => "Plant";

        public string CategoryType => "Foliage";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            var meta = new Dictionary<string, object>();
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

            tags.Add( "plant" );

            return tags;
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            // Hedges with the Plantdata component are not spawnable
            if (prefab.name.ToLower().Contains("hedge"))
                return false;

            return entityManager.HasComponent<PlantData>( entity ) && !entityManager.HasComponent<TreeData>( entity );
        }

        public bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return false;
        }
    }
}
