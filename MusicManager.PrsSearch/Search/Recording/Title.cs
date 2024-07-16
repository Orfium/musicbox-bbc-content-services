using MusicManager.PrsSearch.Recording;
using RecordingServiceReference;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.PrsSearch.Search.Recording
{
    public class Title: RecordingRequest
    {
        public string TrackTitle { get; set; }

        protected override string RequestType { get; } = "title-artist";
        protected override NewRecordingsSearchRequest CreateRequest()
        {
            return new NewRecordingsSearchRequest
            {
                Title = TrackTitle,

                ipType = RecordingInterestedPartyType.Recording_Title
            };
        }

        public static Models.Recording[] GetRecordings(string prsToken,string title, int limit = 20)
        {
            Title search = new Title();
            if (title == null)
                throw new ArgumentNullException(nameof(title));
            if (title.Length == 0)
                throw new ArgumentException("must not be empty", nameof(title));
            if (title.Length > 15)
                throw new ArgumentException($"length must not be greater than 15 (value: '{title}')", nameof(title));

                search = new Title { TrackTitle = title };
            
            var results = search.GetResults(prsToken);

            PrsLogger.LogSearchPerformed(SearchType.Recording, "title", results.Length, title);

            return results;
        }

    }
}
