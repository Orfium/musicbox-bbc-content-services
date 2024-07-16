using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching
{
    public enum MatchResultMode
    {
        All,
        Best
    }

    /// <summary>
    /// Describes an attempted match between two tracks. See TrackCompare for detailed match result.
    /// </summary>
    public class TrackSearchContext
    {
        public int Agid { get; set; }

        public Track SearchTrack { get; set; }
        public Track MatchedTrack { get; set; }

        public TrackCompare TrackCompare { get; set; }


        public static TrackSearchContext[] GetMatches(Track[] searchTracks,
                                                      Track[] candidateTracks,
                                                      MatchConditions matchConditions,
                                                      MatchResultMode matchResultMode)
        {
            return searchTracks.SelectMany(t => GetMatches(t, candidateTracks, matchConditions, matchResultMode))
                               .ToArray();
        }

        public static TrackSearchContext GetMatche(Track Track,
                                                      Track[] candidateTracks,
                                                      MatchConditions matchConditions,
                                                      MatchResultMode matchResultMode)
        {
            return (TrackSearchContext)GetMatches(Track, candidateTracks, matchConditions, matchResultMode);
        }

        public static IEnumerable<TrackSearchContext> GetMatches(
            Track track,
            IEnumerable<Track> candidateTracks,
            MatchConditions matchConditions,
            MatchResultMode matchResultMode)
        {
            var trackComparisons = candidateTracks.Select(d => TrackCompare.Match(track, d, matchConditions))
                                                  .ToArray();

            var allMatches = trackComparisons.Where(c => c.MatchType != MatchType.NotMatched)
                                             .OrderByDescending(x => (int)x.MatchType)
                                             .ThenByDescending(x => x.NormalisedOverallScore)
                                             .ToArray();

            //stats?.UpdateProgress(allMatches.Any());

            var matches = matchResultMode == MatchResultMode.Best ? allMatches.Take(1) : allMatches;

            return matches.Select(m => new TrackSearchContext
            {
                MatchedTrack = m.MatchTrack,
                SearchTrack = track,
                TrackCompare = m
            });
        }

    }
}
