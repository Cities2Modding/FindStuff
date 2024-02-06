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
    }
}
