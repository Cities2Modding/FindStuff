using Game.Prefabs;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class PropHelper(EntityManager entityManager) : IBaseHelper
    {
        public string PrefabType => _PrefabType;
        private string _PrefabType = "PropMisc";

        public string CategoryType {
            get;
            private set;
        } = "Props";

        public Dictionary<string, object> CreateMeta(PrefabBase prefab, Entity entity)
        {
            return new Dictionary<string, object>();
        }

        public List<string> CreateTags(PrefabBase prefab, Entity entity)
        {
            List<string> tags = new List<string>();

            if (entityManager == null)
                return tags;

            CategoryType = "Props";

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
            else if (prefabLowered.Contains("hedge")) // Working hedges are found here
            {
                tags.Add("plant");
                tags.Add("foliage");
                _PrefabType = "Plant";
                CategoryType = "Foliage";
            }
            else if (prefabLowered.Contains("light") && !prefabLowered.Contains("traffic"))
            {
                tags.Add("light");
                tags.Add("accessory");
                _PrefabType = "Accessory";
            }
            else if (prefabLowered.Contains("bench"))
            {
                tags.Add("bench");
                tags.Add("accessory");
                _PrefabType = "Accessory";
            }
            else if (prefabLowered.Contains("table") || prefabLowered.Contains("tableset"))
            {
                tags.Add("table");
                tags.Add("accessory");
                _PrefabType = "Accessory";
            }
            else if (prefabLowered.Contains("trashbin"))
            {
                tags.Add("trashbin");
                tags.Add("accessory");
                _PrefabType = "Accessory";
            }
            else if (prefabLowered.Contains("gazebo") || prefabLowered.Contains("grill") || prefabLowered.Contains("food store") || prefabLowered.Contains("food cart"))
            {
                tags.Add("accessory");
                _PrefabType = "Accessory";
            }
            else if ( entityManager.HasChunkComponent<BridgeData>( entity ) )
            {
                tags.Add( "bridge" );
            }
            else
            {
                _PrefabType = "PropMisc";
            }

            return tags.OrderBy( t => t ).ToList( );
        }

        public bool IsValidPrefab(PrefabBase prefab, Entity entity)
        {
            if (entityManager == null || entity == Entity.Null)
                return false;

            return prefab is StaticObjectPrefab staticObjectPrefab && staticObjectPrefab.m_Meshes.Length > 0 && staticObjectPrefab.isDirty == true && staticObjectPrefab.active == true && staticObjectPrefab.components.Find(p => p is SpawnableObject);
        }
    }
}
