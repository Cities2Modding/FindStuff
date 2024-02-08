using Colossal.Entities;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class ZoneBuildingHelper(EntityManager entityManager, PrefabSystem prefabSystem) : IBaseHelper
    {
        public string PrefabType => _PrefabType;
        private string _PrefabType = "Unknown";

        public string CategoryType => "Zones";

        public Dictionary<string, object> CreateMeta(PrefabBase prefab, Entity entity)
        {
            Dictionary<string, object> meta = new Dictionary<string, object>();

            try
            {
                BuildingPrefab buildingPrefab = (BuildingPrefab)prefab;
                meta.Add(IBaseHelper.META_ZONE_LOT_DEPTH, buildingPrefab.m_LotDepth);
                meta.Add(IBaseHelper.META_ZONE_LOT_WIDTH, buildingPrefab.m_LotWidth);
                meta.Add(IBaseHelper.META_ZONE_LOT_SUM, buildingPrefab.m_LotDepth * buildingPrefab.m_LotWidth);
            } catch (Exception e)
            {
                UnityEngine.Debug.Log("ZoneBuildingHelper.CreateMeta: " + e);
            }

            return meta;
        }

        public List<string> CreateTags(PrefabBase prefab, Entity entity)
        {
            List<string> tags = new List<string>();

            if (entityManager == null)
                return new List<string>();

            SpawnableBuildingData spawnableBuildingData = entityManager.GetComponentData<SpawnableBuildingData>(entity);
            ZoneData zoneData = entityManager.GetComponentData<ZoneData>(spawnableBuildingData.m_ZonePrefab);

            switch (zoneData.m_AreaType)
            {
                case Game.Zones.AreaType.Commercial:
                    _PrefabType = "ZoneCommercial";
                    break;
                case Game.Zones.AreaType.Residential:
                    _PrefabType = "ZoneResidential";
                    break;
                case Game.Zones.AreaType.Industrial:
                    ZonePrefab zonePrefab = prefabSystem.GetPrefab<ZonePrefab>(spawnableBuildingData.m_ZonePrefab);
                    _PrefabType = zonePrefab.m_Office ? "ZoneOffice" : "ZoneIndustrial";
                    break;
            }

            return tags;
        }

        public bool IsValidPrefab(PrefabBase prefab, Entity entity)
        {
            if (entityManager == null || entity == Entity.Null)
                return false;

            return prefab is BuildingPrefab
                && entityManager.TryGetComponent(entity, out SpawnableBuildingData spawnableBuildingData)
                && spawnableBuildingData.m_ZonePrefab != Entity.Null
                && entityManager.HasComponent<ZoneData>(spawnableBuildingData.m_ZonePrefab);
        }

        public string GetZoneTypeIcon( PrefabBase prefab, Entity entity )
        {
            if ( prefab is BuildingPrefab
                && entityManager.TryGetComponent( entity, out SpawnableBuildingData spawnableBuildingData )
                && spawnableBuildingData.m_ZonePrefab != Entity.Null
                && entityManager.TryGetComponent<ZoneData>( spawnableBuildingData.m_ZonePrefab, out var zoneData ) )
            {
                var zonePrefab = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>( ).GetPrefab<ZonePrefab>( spawnableBuildingData.m_ZonePrefab );
                
                var name = zonePrefab.name;

                if ( name.StartsWith( "EU " ) || name.StartsWith( "NA " ) )
                    name = name[3..];

                name = name.Replace( " ", "" );

                if ( name == "IndustrialAgriculture" )
                    name = "ZoneAgricultureArea";
                else if ( name == "IndustrialOil" )
                    name = "Oil";
                else if ( name == "IndustrialOre" )
                    name = "ZoneOreArea";
                else
                    name = "Zone" + name;

                return $"Media/Game/Icons/{name}.svg";
            }
            return null;
        }
    }
}
