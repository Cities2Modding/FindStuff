﻿using FindStuff.Indexing;
using Gooee.Plugins;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FindStuff.UI
{
    public class FindStuffViewModel : Model
    {
        public bool IsVisible
        {
            get;
            set;
        }

        public bool IsPicking
        {
            get;
            set;
        }

        public bool IsHistorical
        {
            get;
            set;
        } = false;

        public List<Category> Categories
        {
            get;
            set;
        } = PrefabIndexer._filterMappings.Select( kvp => new Category
        { 
            Filter = kvp.Key, SubFilters = kvp.Value.ToList()
        } ).ToList();

        public bool IsWaitingQuery
        {
            get;
            set;
        }

        // This list gets huge don't serialise it and lag JS!
        [JsonIgnore]
        public List<PrefabItem> Prefabs
        {
            get;
            set;
        } = new List<PrefabItem>( );

        public bool OrderByAscending
        {
            get;
            set;
        } = true;

        public ViewMode ViewMode
        {
            get;
            set;
        } = ViewMode.IconGrid;

        public string Search
        {
            get;
            set;
        }

        public Filter Filter
        {
            get;
            set;
        } = Filter.None;

        public SubFilter SubFilter
        {
            get;
            set;
        } = SubFilter.None;

        public List<string> Favourites
        {

            get;
            set;
        } = new List<string>( );

        public ViewOperationMode OperationMode
        {
            get;
            set;
        } = ViewOperationMode.HideFindStuff;

        public bool EnableShortcut
        {
            get;
            set;
        }

        public bool ExpertMode
        {
            get;
            set;
        }

        public bool Shifted
        {
            get;
            set;
        }

        public PrefabItem Selected
        {
            get;
            set;
        }

        public bool SearchWords
        {
            get;
            set;
        }

        public bool SearchTags
        {
            get;
            set;
        }

        public HashSet<string> RecentSearches
        {
            get;
            set;
        }
    }

    public class PrefabItem
    {
        public int ID
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Category
        {
            get;
            set;
        }

        public string Type
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public string Thumbnail
        {
            get;
            set;
        }

        public string TypeIcon
        {
            get;
            set;
        }

        public bool IsModded
        {
            get;
            set;
        }

        public bool IsExpertMode
        {
            get;
            set;
        }

        public Dictionary<string, object> Meta
        {
            get;
            set;
        }

        public List<string> Tags
        {
            get;
            set;
        }
    }

    public class Category
    {
        public Filter Filter
        {

            get;
            set;
        }

        public List<SubFilter> SubFilters
        {
            get;
            set;
        }
    }

    public enum ViewOperationMode
    {
        HideFindStuff,
        MoveFindStuff,
        HideAssetMenu,
        HideFindStuffSideMenu
    }

    public enum ViewMode
    {
        Rows = 0,
        Columns = 2,
        IconGrid = 3,
        IconGridLarge = 4,
        Detailed = 5
    }

    public enum Filter
    {
        None = 0,
        Favourite = 1,

        // Sub Filter Groups (Prefab Category)
        Foliage = 2,
        Buildings = 4,
        Zones = 5,
        Props = 6,
        Misc = 20,

        // Top Level
        Network = 3,
    }

    public enum SubFilter : long
    {
        None = 0,
        ZoneResidential,
        ZoneCommercial,
        ZoneOffice,
        ZoneIndustrial,

        // Buildings
        ServiceBuilding,
        SignatureBuilding,
        MiscBuilding,

        Park,
        Parking,

        Vehicle,
        Tree,
        Plant,
        PropMisc,
        Surface,
        Area,

        SignsAndPosters,
        Fences,
        Billboards,
        Accessory,

        // Networks
        RoadTool,
        SmallRoad,
        MediumRoad,
        LargeRoad,
        Highway,
        Pavement,
        Roundabout,
        OtherNetwork,

        // Misc
        TransportStop
    }

    public enum SearchSpeed
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }
}
