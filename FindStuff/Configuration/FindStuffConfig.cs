using FindStuff.UI;
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
    }
}
