using System;
using System.Collections.Generic;
using System.Linq;

namespace FindStuff.Indexing
{
    internal class WorkingSearchData
    {
        public string Key
        {
            get;
            set;
        }

        public string CurrentSearch
        {
            get;
            set;
        }

        public HashSet<(int ID, int Score)> Data
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

        public void Add( int prefabID, Func<string, int, (bool IsMatch, int Score)> onDoSearch )
        {
            if ( prefabID < 0 )
                return;

            var hasSearch = !string.IsNullOrEmpty( Parameters.Search );

            if ( Data.Count( d => d.ID == prefabID ) > 0 )
                return;

            if ( hasSearch )
            {
                var searchResult = onDoSearch( Parameters.Search, prefabID );

                if ( searchResult.IsMatch )
                    Data.Add( (prefabID, searchResult.Score) );
                // else don't include in results
            }
            else
                Data.Add( (prefabID, 0) );
        }
    }
}
