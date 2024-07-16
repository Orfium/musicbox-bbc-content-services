using MusicManager.PrsSearch;
using System;
using System.Linq;
using WorksServiceReference;

namespace Soundmouse.Matching.Prs.Search.Work
{
    public class Title : WorkRequest
    {
        public const int MaxLength = 50;


        public override int Limit => 1000;

        public string Value { get; set; }


        protected override NewWorkSearchRequest CreateRequest()
        {
            return new NewWorkSearchRequest
            {
                ipType = InterestedPartyType.Work_Title,
                title = Value
            };
        }


 

        public static MusicManager.PrsSearch.Models.Work[] GetWorks(string prsToken,string[] titles)
        {
            return titles.Where(title => !string.IsNullOrEmpty(title))
                         .Select(title => title.TruncateWords(MaxLength))
                         .Distinct()
                         .SelectMany(title => new Title { Value = title }.GetResults(prsToken))
                         .ToArray();
        }
    }
} 