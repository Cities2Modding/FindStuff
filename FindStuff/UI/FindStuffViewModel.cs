using Gooee.Plugins;
using Gooee.Plugins.Attributes;
using System.Collections.Generic;

namespace FindStuff.UI
{
    public class FindStuffViewModel : Model
    {
        public bool IsVisible
        {
            get;
            set;
        }

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
        public string Name
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
        Foliage = 2,
        Network = 3,
        Buildings = 4,
        Zones = 5,
        Surface = 6,
        Misc = 20,
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
