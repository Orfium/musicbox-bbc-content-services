using System;
using System.Linq;
using WorksServiceReference;
using Soundmouse.Messaging.Model;

namespace Soundmouse.Matching.Prs.Search.Work
{
    public class TitleWriter : WorkRequest
    {
        public override int Limit => 1000;

        public string Title { get; set; }
        public string[] Writers { get; set; }


        protected override NewWorkSearchRequest CreateRequest()
        {
            return new NewWorkSearchRequest
            {
                ipType = InterestedPartyType.Work_Title_Writer_Name,
                title = Title,
                WriterName1 = Writers.Length > 0 ? Writers[0] : "",
                WriterName2 = Writers.Length > 1 ? Writers[1] : "",
                WriterName3 = Writers.Length > 2 ? Writers[2] : "",
                WriterName4 = Writers.Length > 3 ? Writers[3] : "",
                WriterName5 = Writers.Length > 4 ? Writers[4] : ""
            };
        }

        public bool Equals(TitleWriter other)
        {
            return string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) &&
                   Writers.Length == other.Writers.Length &&
                   Writers.SequenceEqual(other.Writers, StringComparer.OrdinalIgnoreCase);
        }


        public static bool TryCreateFromTrack(EntityVersion<Messaging.Model.Track> track, out TitleWriter request)
        {
            var composers = track.Value.TrackData.InterestedParties
                                 .GetByRole("composer")
                                 .ToArray();

            if (!composers.Any())
            {
                request = null;
                return false;
            }

            if (composers.Length > 5)
            {
                //Serilog.Log.Logger.Debug(
                //    "Search type: {Prs_SearchType}. Query: {Prs_Query}. Track version: {VersionId}. Limiting search to the first 5 composers.",
                //    SearchType.Work,
                //    "title_writer",
                //    track.VersionId);
            }

            var writers = composers.Take(5)
                                   .Select(p => p.FullName.Truncate(50))
                                   .ToArray();

            request = new TitleWriter
            {
                Title = track.Value.TrackData.Title.Truncate(50),
                Writers = writers
            };

            return true;
        }


        public static MusicManager.PrsSearch.Models.Work[] GetWorks(string prsToken, string title, string writers, int limit = 50)
        {
            TitleWriter search = new TitleWriter();
            if (title == null)
                throw new ArgumentNullException(nameof(title));
            if (title.Length == 0)
                throw new ArgumentException("must not be empty", nameof(title));
            if (title.Length > limit)
                title = title.Substring(0, limit);

            if (writers != null)
            {
                if (writers.Length > limit)
                    writers = writers.Substring(0, limit);

                search = new TitleWriter { Title = title, Writers = writers.Split(',', StringSplitOptions.RemoveEmptyEntries) };
            }
            else
            {
                search = new TitleWriter { Title = title, Writers = null };
            }
            var results = search.GetResults(prsToken);          

            return results;
        }
    }
}