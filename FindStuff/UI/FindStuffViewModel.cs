using Gooee.Plugins;
using Gooee.Plugins.Attributes;
using Newtonsoft.Json;
using System;
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

        public List<Category> Categories
        {

            get;
            set;
        } = [
                new Category 
                { 
                    Filter = Filter.Foliage, 
                    SubFilters = 
                    [
                        SubFilter.Tree, 
                        SubFilter.Plant
                    ] 
                },
                new Category 
                { 
                    Filter = Filter.Buildings, 
                    SubFilters = 
                    [
                        SubFilter.ServiceBuilding, 
                        SubFilter.SignatureBuilding
                    ] 
                },
                new Category 
                { 
                    Filter = Filter.Zones,
                    SubFilters = 
                    [
                        SubFilter.ZoneResidential, 
                        SubFilter.ZoneCommercial, 
                        SubFilter.ZoneIndustrial,
                        SubFilter.ZoneOffice
                    ] 
                },
                new Category 
                { 
                    Filter = Filter.Misc,
                    SubFilters = 
                    [
                        SubFilter.Prop, 
                        SubFilter.Vehicle
                    ] 
                }
            ];

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
        } = ViewMode.Rows;

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
        Misc = 20,

        // Top Level
        Network = 3,
        Surface = 6,
    }

    public enum SubFilter
    {
        None = 0,
        ZoneResidential = 1,
        ZoneCommercial = 2,
        ZoneOffice = 3,
        ZoneIndustrial = 4,
        ServiceBuilding = 5,
        SignatureBuilding = 6,
        Vehicle = 7,
        Tree = 8,
        Plant = 9,
        Prop = 10,
    }
}
