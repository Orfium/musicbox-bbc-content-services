using MusicManager.PrsSearch;
using RecordingServiceReference;
using System;

namespace MusicManager.PrsSearch.Recording
{
    public class Isrc : RecordingRequest
    {
        public string Value { get; set; }

        protected override string RequestType { get; } = "isrc";


        protected override NewRecordingsSearchRequest CreateRequest()
        {
            return new NewRecordingsSearchRequest
            {
                ipType = RecordingInterestedPartyType.ISRC_No,
                IsrcNo = Value
            };
        }


        public static Models.Recording[] GetRecordings(string prsToken,string isrc, int limit = 20)
        {
            if (isrc == null)
                throw new ArgumentNullException(nameof(isrc));
            if (isrc.Length == 0)
                throw new ArgumentException("must not be empty", nameof(isrc));
            if (isrc.Length > 15)
                throw new ArgumentException($"length must not be greater than 15 (value: '{isrc}')", nameof(isrc));
            
            var search = new Isrc {Value = isrc};
            
            var results = search.GetResults(prsToken);

            return results;
        }
    }
}