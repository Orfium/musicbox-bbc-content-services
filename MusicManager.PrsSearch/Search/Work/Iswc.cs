using Soundmouse.Messaging.Model;
using System;
using WorksServiceReference;

namespace Soundmouse.Matching.Prs.Search.Work
{
    public class Iswc : WorkRequest
    {
        public string Value { get; set; }


        protected override NewWorkSearchRequest CreateRequest()
        {
            return new NewWorkSearchRequest
            {
                ipType = InterestedPartyType.ISWC_No,
                ISWC = Value
            };
        }

        public static Iswc CreateFromTrack(Track track)
        {
            if (track == null)
                throw new ArgumentNullException(nameof(track));

            string value;

            if (!track.TrackData.Identifiers.TryGetValue("iswc", out value))
            {
                throw new ApplicationException();
            }
            return new Iswc {Value = value};
        }
    }
}