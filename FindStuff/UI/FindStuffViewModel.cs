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
        Foliage = 1,
        Roads = 2,
        Signature = 3,
        ZoneResidential = 4,
        ZoneCommercial = 5,
        ZoneIndustrial = 6, 
        Vehicles
    }
}
