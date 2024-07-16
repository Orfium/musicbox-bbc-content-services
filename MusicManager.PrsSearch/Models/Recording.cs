using RecordingServiceReference;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;

namespace MusicManager.PrsSearch.Models
{
    public class Recording
    {
        public string AlternateTitle { get; set; }
        public ICollection<Soundmouse.Messaging.Model.InterestedParty> Artists { get; set; }
        public float? Duration { get; set; }
        public string Isrc { get; set; }
        public string Iswc { get; set; }
        public string MedleyTitle { get; set; }
        public string MusicOrigin { get; set; }
        public int RecordingId { get; set; }
        public string Title { get; set; }
        public string Tunecode { get; set; }
        public string[] Writers { get; set; }
        public int? YearProduced { get; set; }
        public int? YearRecorded { get; set; }
        public string ProductCatNo { get; set; }
        public string ProductTitle { get; set; }
        public WebServiceRecordingsSummaryBO arg { get; set; }


        public override string ToString()
        {
            return Title;
        }

        public Track ToTrack()
        {
            var track = new Track
            {
                Source = new Source
                {
                    Updated = DateTime.UtcNow,
                    Created = DateTime.UtcNow,
                    CreateMethod = "prs-service",
                    UpdateMethod = "prs-service"
                },
                Territories = new[] { "GB" },
                TrackData = new TrackData
                {
                    AlternativeTitle = AlternateTitle,
                    Duration = Duration,
                    Identifiers = GetIdentifiers(),
                    InterestedParties = Artists,
                    //Miscellaneous = new Dictionary<string, string> { { Soundmouse.Messaging.Model.Miscellaneous.PrsRecordingOrigin, MusicOrigin } },
                    Title = Title,
                    Product = new Soundmouse.Messaging.Model.Product() { 
                        Identifiers = GetProductIdentifiers(),
                        Name = ProductTitle
                    }
                }                
            };

            return track;
        }

        private Dictionary<string, string> GetProductIdentifiers()
        {
            var identifiers = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(ProductCatNo))
                identifiers["catalogue_number"] = ProductCatNo;
           
            return identifiers;
        }

        private Dictionary<string, string> GetIdentifiers()
        {
            var identifiers = new Dictionary<string, string>();

            var prs = CleanseIdentifier(Tunecode);

            if (!string.IsNullOrEmpty(prs))
                identifiers["prs"] = prs;

            var isrc = CleanseIdentifier(Isrc);

            if (!string.IsNullOrEmpty(isrc))
                identifiers["isrc"] = isrc;

            identifiers["prs:recording"] = RecordingId.ToString();
            return identifiers;
        }

        private static string CleanseIdentifier(string original)
        {
            return original?.Replace("-", "")
                            .Replace(".", "")
                            .Replace(" ", "")
                            .Trim();
        }
    }
}