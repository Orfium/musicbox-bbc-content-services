using MusicManager.PrsSearch;
using Soundmouse.Matching.Prs.Search.Work;
using System;
using WorksServiceReference;

namespace MusicManager.PrsSearch.Work
{
    public class Tunecode : WorkRequest
    {
        public string Value { get; set; }


        protected override NewWorkSearchRequest CreateRequest()
        {
            return new NewWorkSearchRequest
            {
                ipType = InterestedPartyType.Tunecode,
                Tunecode = Value
            };
        }


        public static MusicManager.PrsSearch.Models.Work[] GetWorks(string prsToken,string tunecode, int limit = 10)
        {
            if (tunecode == null)
                throw new ArgumentNullException(nameof(tunecode));
            if (tunecode.Length == 0)
                throw new ArgumentException("tunecode must not be empty", nameof(tunecode));

            var search = new Tunecode {Value = tunecode};

            var results = search.GetResults(prsToken);

            PrsLogger.LogSearchPerformed(SearchType.Work, "tunecode", results.Length, tunecode);

            return results;
        }
    }
}