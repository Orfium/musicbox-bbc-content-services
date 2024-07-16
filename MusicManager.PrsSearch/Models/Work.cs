using System;
using System.Collections.Generic;
using System.Linq;
using MusicManager.Core.ViewModules;
using MusicManager.PrsSearch;
using Soundmouse.Messaging.Model;
using WorkDetailsServiceReference;
using WorksServiceReference;

namespace MusicManager.PrsSearch.Models
{
    public enum WorkType
    {
        Blank = 0,
        Commissioned = 1,
        Library = 2,
        LibraryUnconfirmed = 3
    }

    public class Work
    {
        public string[] Iswc { get; set; }
        public string[] LibraryCatalogueNumbers { get; set; }
        public MLInterestedParties[] Publishers { get; set; }
        public string Title { get; set; }
        public string Tunecode { get; set; }
        public string Type { get; set; }
        public MLInterestedParties[] Writers { get; set; }
        public WorkShareSummaryBO[] MechanicalShareSummary { get; set; }
        public string PriorApprovalCode { get; set; }
        public WebServiceWorkSummaryBO arg2 { get; set; }
       

        public override string ToString()
        {
            return Title;
        }

        public Soundmouse.Messaging.Model.Track ToTrack()
        {
            var track = new Soundmouse.Messaging.Model.Track
            {
                Source = new Source
                {
                    Updated = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    CreateMethod = "prs-service",
                    UpdateMethod = "prs-service"
                },
                Territories = new[] {"GB"},
                TrackData = new TrackData
                {
                    Identifiers = new Dictionary<string, string> {{"prs", Tunecode}},
                    InterestedParties = new List<Soundmouse.Messaging.Model.InterestedParty>(),
                    Miscellaneous = new Dictionary<string, string>(),
                    Title = Title
                }
            };

            return ToTrack(track, true);
        }

        public Soundmouse.Messaging.Model.Track ToTrack(Soundmouse.Messaging.Model.Track track, bool isWorkOnly = false)
        {
            var trackData = track.TrackData;

            if (!string.IsNullOrEmpty(Iswc.FirstOrDefault()))
                trackData.Identifiers["iswc"] = Iswc.First();

            foreach (var publisher in Publishers)
                trackData.InterestedParties.Add(publisher);

            foreach (var writer in Writers)
                trackData.InterestedParties.Add(writer);

            // do not set music origin
            trackData.MusicOrigin = null;
            //trackData.Miscellaneous[Miscellaneous.PrsIntendedUse] = Type; ///DHARSHANA

            if (isWorkOnly)
                return track;

            trackData.AlternativeTitle = Title;

            // if the title is equal (other than case)
            if (CanFixCase(Title, trackData.Title))
            {
                // leave the recording's title in place
                if (trackData.AlternativeTitle == null)
                    trackData.AlternativeTitle = Title;
            }
            else
            {
                // work title has precedence over recording title (recording title becomes alt)
                trackData.AlternativeTitle = trackData.Title;
                trackData.Title = Title;
            }

            return track;
        }

        /// <summary>
        /// Determine if a string is in upper case but a normal/mixed case version exists.
        /// </summary>
        private bool CanFixCase(string main, string alt)
        {
            if (string.IsNullOrEmpty(main) || string.IsNullOrEmpty(alt))
                return false;

            if (!string.Equals(main, alt, StringComparison.OrdinalIgnoreCase))
                return false;

            if (main.Any(char.IsLower))
                return false;

            var anyLower = alt.Any(char.IsLower);
            var anyUpper = alt.Any(char.IsUpper);

            // alt must be mixed-case
            return anyLower && anyUpper;
        }
    }
}