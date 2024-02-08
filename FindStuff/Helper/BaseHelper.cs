using Game.Prefabs;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public interface IBaseHelper
    {
        const string META_IS_DANGEROUS = "IsDangerous";
        const string META_IS_DANGEROUS_REASON = "IsDangerousReason";
        const string META_IS_SPAWNABLE = "IsSpawnable";
        const string META_ZONE_LEVEL = "ZoneLevel";
        const string META_ZONE_LOT_DEPTH = "ZoneLotDepth";
        const string META_ZONE_LOT_WIDTH = "ZoneLotWidth";
        const string META_ZONE_LOT_SUM = "ZoneLotSum";

        public string PrefabType { get; }
        public string CategoryType { get; }

        bool IsValidPrefab(PrefabBase prefab, Entity entity);
        List<string> CreateTags(PrefabBase prefab, Entity entity);
        Dictionary<string, object> CreateMeta(PrefabBase prefab, Entity entity);
    }
}
