using Game.Prefabs;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class PropHelper( EntityManager entityManager ) : BaseHelper
    {
        public override string PrefabType => "PropMisc";

        public override string CategoryType
        {
            get;
            protected set;
        } = "Props";

        public override Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
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

        public override List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            CategoryType = "Props";

            tags.Add( "prop" );

            string prefabLowered = prefab.name.ToLower( );
            if ( prefabLowered.StartsWith( "sign" ) )
            {
                PrefabType = "SignsAndPosters";
                tags.Add( "sign" );
            }
            else if ( prefabLowered.StartsWith( "poster" ) )
            {
                tags.Add( "poster" );
                PrefabType = "SignsAndPosters";
            }
            else if ( prefabLowered.StartsWith( "billboard" ) )
            {
                tags.Add( "billboard" );
                PrefabType = "Billboards";
            }
            else if ( prefabLowered.StartsWith( "fence" ) )
            {
                tags.Add( "fence" );
                PrefabType = "Fences";
            }
            else if ( prefabLowered.Contains( "hedge" ) ) // Working hedges are found here
            {
                tags.Add( "plant" );
                tags.Add( "foliage" );
                PrefabType = "Plant";
                CategoryType = "Foliage";
            }
            else if ( prefabLowered.Contains( "light" ) && !prefabLowered.Contains( "traffic" ) )
            {
                tags.Add( "light" );
                tags.Add( "accessory" );
                PrefabType = "Accessory";
            }
            else if ( prefabLowered.Contains( "bench" ) )
            {
                tags.Add( "bench" );
                tags.Add( "accessory" );
                PrefabType = "Accessory";
            }
            else if ( prefabLowered.Contains( "table" ) || prefabLowered.Contains( "tableset" ) )
            {
                tags.Add( "table" );
                tags.Add( "accessory" );
                PrefabType = "Accessory";
            }
            else if ( prefabLowered.Contains( "trashbin" ) )
            {
                tags.Add( "trashbin" );
                tags.Add( "accessory" );
                PrefabType = "Accessory";
            }
            else if ( prefabLowered.Contains( "gazebo" ) || prefabLowered.Contains( "grill" ) || prefabLowered.Contains( "food store" ) || prefabLowered.Contains( "food cart" ) )
            {
                tags.Add( "accessory" );
                PrefabType = "Accessory";
            }
            else if ( entityManager.HasChunkComponent<BridgeData>( entity ) )
            {
                tags.Add( "bridge" );
            }
            else
            {
                PrefabType = "PropMisc";
            }

            return tags.OrderBy( t => t ).ToList( );
        }

        public override bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return prefab is StaticObjectPrefab staticObjectPrefab && staticObjectPrefab.m_Meshes.Length > 0 && staticObjectPrefab.isDirty == true && staticObjectPrefab.active == true && (staticObjectPrefab.components.Count( p => p is SpawnableObject ) > 0 || staticObjectPrefab.name.Contains("Random"));
        }

        public override bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return false;
        }
    }
}
