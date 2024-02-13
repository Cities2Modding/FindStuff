using System;
using System.Collections.Generic;

namespace FindStuff.Indexing
{
    internal class WorkingSearchData
    {
        public string Key
        {
            get;
            set;
        }

        public HashSet<int> Data
        {
            get;
            set;
        } = [];

        public (string Json, int Count) Cache
        {
            get;
            set;
        }

        public (string Search, bool OrderByAscending) Parameters
        {
            get;
            set;
        } = ("", true);

        public Action<string, (string Json, int Count)> OnComplete
        {
            get;
            set;
        }

        public void Add( int prefabID, Func<string, int, bool> onDoSearch )
        {
            if ( prefabID < 0 )
                return;

            var hasSearch = !string.IsNullOrEmpty( Parameters.Search );

            if ( Data.Contains( prefabID ) )
                return;

            if ( hasSearch )
            {
                if ( onDoSearch( Parameters.Search, prefabID ) )
                    Data.Add( prefabID );
                // else don't include in results
            }
            else
                Data.Add( prefabID );
        }
    }
}
