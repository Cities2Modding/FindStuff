using FindStuff.UI;
using System;
using System.Collections.Generic;

namespace FindStuff.Configuration
{
    public class FindStuffConfig : ConfigBase
    {
        protected override string ConfigFileName => "config.json";

        public HashSet<string> Favourites
        {

            get;
            set;
        } = new HashSet<string>( );

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

        public bool EnableShortcut
        {
            get;
            set;
        } = false;

        public bool ExpertMode
        {
            get;
            set;
        } = false;

        public DateTime LastSearchHistoryPurge
        {
            get;
            set;
        }

        public Dictionary<string, ushort> RecentSearches
        {
            get;
            set;
        } = [];
    }
}
