using Game.Prefabs;
using System;
using System.Collections.Generic;
using Unity.Entities;

namespace FindStuff.Helper
{
    public abstract partial class BaseHelper : IBaseHelper
    {
        protected const string META_IS_DANGEROUS = "IsDangerous";
        protected const string META_IS_DANGEROUS_REASON = "IsDangerousReason";
        protected const string META_IS_SPAWNABLE = "IsSpawnable";
        protected const string META_ZONE_LEVEL = "ZoneLevel";
        protected const string META_ZONE_LOT_DEPTH = "ZoneLotDepth";
        protected const string META_ZONE_LOT_WIDTH = "ZoneLotWidth";
        protected const string META_ZONE_LOT_SUM = "ZoneLotSum";
        protected const string META_BUILDING_STATIC_UPGRADE = "BuildingStaticUpgrade";

        public virtual string PrefabType
        { 
            get; 
            protected set;
        }

        public virtual string CategoryType
        {
            get;
            protected set;
        }

        public abstract Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity );

        public abstract List<String> CreateTags( PrefabBase prefab, Entity entity );

        public abstract Boolean IsExpertMode( PrefabBase prefab, Entity entity );

        public abstract Boolean IsValidPrefab( PrefabBase prefab, Entity entity );
    }

    public interface IBaseHelper
    {
        public string PrefabType
        {
            get;
        }
        public string CategoryType
        {
            get;
        }

        bool IsValidPrefab( PrefabBase prefab, Entity entity );
        bool IsExpertMode( PrefabBase prefab, Entity entity );
        List<string> CreateTags( PrefabBase prefab, Entity entity );
        Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity );
    }
}
