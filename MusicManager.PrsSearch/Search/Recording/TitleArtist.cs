using MusicManager.PrsSearch;
using MusicManager.PrsSearch.DataMatching;
using RecordingServiceReference;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicManager.PrsSearch.Recording
{
    public class TitleArtist : RecordingRequest
    {
        public string[] Artists { get; set; }
        public string Title { get; set; }

        protected override string RequestType { get; } = "title-artist";


        protected override NewRecordingsSearchRequest CreateRequest()
        {
            string artist1 = "", artist2 = "";
            if (Artists != null && Artists.Length > 0)
            {
                if (Artists.Length > 0)
                    artist1 = Artists[0];

                if (Artists.Length > 1)
                    artist2 = Artists[1];
            }


            return new NewRecordingsSearchRequest
            {
                Title = Title,
                ArtistName1 = artist1,
                ArtistName2 = artist2,
                ipType = RecordingInterestedPartyType.Title_Artist_Name
            };
        }


        public static Models.Recording[] GetRecordings(string prsToken,string title, string artists, int limit = 20)
        {
            TitleArtist search = new TitleArtist();
            if (title == null)
                throw new ArgumentNullException(nameof(title));
            if (title.Length == 0)
                throw new ArgumentException("must not be empty", nameof(title));
            if (title.Length > 50)
                title = title.Substring(0, 50);           

            //if (artists == null)
            //{
            //    throw new ArgumentException($"There are no artists (value: '{artists}')", nameof(artists));
            //}
            //if (artists != null)
            //{
            //    if (artists.Split(',').Length > 2)
            //        throw new ArgumentException($"Only 2 Artists can be added (value: '{artists}')", nameof(artists));
            //}
            if (artists != null)
            {
                if (artists.Length > 50)
                    artists = artists.Substring(0, 50);

                search = new TitleArtist { Title = title, Artists = artists.Split(',',StringSplitOptions.RemoveEmptyEntries)};
            }
            else
            {
                search = new TitleArtist { Title = title, Artists = null };
            }
            var results = search.GetResults(prsToken);

            return results;
        }

        public static Track GetTrackMatches(Track mlTrack, Track[] prsTracks)
        {
            Track[] sourceTrack = { mlTrack };            

            TrackSearchContext[] trackSearchContexts = TrackSearchContext.GetMatches(sourceTrack, prsTracks, MatchConditions.PRS, MatchResultMode.Best);
            if (trackSearchContexts.Length > 0)
                return trackSearchContexts[0].MatchedTrack;

            return null;
        }

        public static Track GetRecordingMatches(Track track, Models.Recording[] recordings)
        {
            Track[] _sourceTrack = { track };            

            Track[] _tracks = recordings.Select(a => a.ToTrack()).ToArray();

            TrackSearchContext[] trackSearchContexts = TrackSearchContext.GetMatches(_sourceTrack, _tracks, MatchConditions.PRS, MatchResultMode.Best);
            if (trackSearchContexts.Length > 0) 
                return trackSearchContexts[0].MatchedTrack;

            return null;
        }


        public bool Equals(TitleArtist other)
        {
            return string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) &&
                   Artists.Length == other.Artists.Length &&
                   Artists.SequenceEqual(other.Artists, StringComparer.OrdinalIgnoreCase);
        }

    }
}