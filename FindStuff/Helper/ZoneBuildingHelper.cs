using Colossal.Entities;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public class ZoneBuildingHelper( EntityManager entityManager, PrefabSystem prefabSystem ) : IBaseHelper
    {
        public string PrefabType => _PrefabType;
        private string _PrefabType = "Unknown";

        public string CategoryType => "Zones";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            Dictionary<string, object> meta = new Dictionary<string, object>( );

            try
            {
                SpawnableBuildingData spawnableBuildingData = entityManager.GetComponentData<SpawnableBuildingData>( entity );
                BuildingPrefab buildingPrefab = ( BuildingPrefab ) prefab;

                meta.Add( IBaseHelper.META_ZONE_LEVEL, spawnableBuildingData.m_Level );
                meta.Add( IBaseHelper.META_ZONE_LOT_DEPTH, buildingPrefab.m_LotDepth );
                meta.Add( IBaseHelper.META_ZONE_LOT_WIDTH, buildingPrefab.m_LotWidth );
                meta.Add( IBaseHelper.META_ZONE_LOT_SUM, buildingPrefab.m_LotDepth * buildingPrefab.m_LotWidth );

                var placeableObject = prefab.GetComponent<PlaceableObject>( );

                if ( placeableObject != null )
                {
                    meta.Add( "Cost", placeableObject.m_ConstructionCost );
                    meta.Add( "XPReward", placeableObject.m_XPReward );
                }
            }
            catch ( Exception e )
            {
                UnityEngine.Debug.Log( "ZoneBuildingHelper.CreateMeta: " + e );
            }

            return meta;
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            SpawnableBuildingData spawnableBuildingData = entityManager.GetComponentData<SpawnableBuildingData>( entity );
            ZoneData zoneData = entityManager.GetComponentData<ZoneData>( spawnableBuildingData.m_ZonePrefab );
            var zonePrefab = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>( )
                .GetPrefab<ZonePrefab>( spawnableBuildingData.m_ZonePrefab );

            var buildingPrefab = ( BuildingPrefab ) prefab;

            tags.Add( $"{buildingPrefab.m_LotWidth}x{buildingPrefab.m_LotDepth}" );
            tags.Add( "zones" );

            switch ( zoneData.m_AreaType )
            {
                case Game.Zones.AreaType.Commercial:
                    _PrefabType = "ZoneCommercial";
                    tags.Add( "commercial" );
                    break;
                case Game.Zones.AreaType.Residential:
                    _PrefabType = "ZoneResidential";
                    tags.Add( "residential" );
                    break;
                case Game.Zones.AreaType.Industrial:
                    _PrefabType = zonePrefab.m_Office ? "ZoneOffice" : "ZoneIndustrial";
                    tags.Add( zonePrefab.m_Office ? "office" : "industrial" );
                    break;
            }

            var name = zonePrefab.name;
            var theme = "";

            if ( name.StartsWith( "EU " ) || name.StartsWith( "NA " ) )
                theme = name[..2].ToLower( );

            if ( !string.IsNullOrEmpty( theme ) )
                tags.Add( theme );

            if ( name.ToLower( ).Contains( " high" ) )
                tags.Add( "high-density" );
            else if ( name.ToLower( ).Contains( " low" ) )
                tags.Add( "low-density" );
            else if ( name.ToLower( ).Contains( " mixed" ) )
                tags.Add( "mixed-density" );
            else if ( name.ToLower( ).Contains( " lowrent" ) )
                tags.Add( "low-rent" );
            else if ( name.ToLower( ).Contains( " medium row" ) )
            {
                tags.Add( "medium-density" );
                tags.Add( "row" );
            }
            else if ( name.ToLower( ).Contains( " medium" ) )
                tags.Add( "medium-density" );


            return tags;
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || entity == Entity.Null )
                return false;

            return prefab is BuildingPrefab
                && entityManager.TryGetComponent( entity, out SpawnableBuildingData spawnableBuildingData )
                && spawnableBuildingData.m_ZonePrefab != Entity.Null
                && entityManager.HasComponent<ZoneData>( spawnableBuildingData.m_ZonePrefab );
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

        public bool IsExpertMode( PrefabBase prefab, Entity entity )
        {
            return false;
        }
    }
}
