using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ExternalApiModels
{
    public class ExtTrack
    {
        public Guid smTrackId { get; set; }
        public string extRefTrackId { get; set; }
        public string albumTitle { get; set; }
        public string albumArtist { get; set; }
        public string title { get; set; }
        public List<InterestedParties> interestedParties { get; set; }
        public float? duration { get; set; }
        public string musicOrigin { get; set; }
        public IDictionary<string, string> albumIdentifiers { get; set; }
        public IDictionary<string, string> trackIdentifiers { get; set; }
        public ICollection<string> albumTags { get; set; }
        public List<TypeValue> albumTagExtended { get; set; }
        public ICollection<string> keywords { get; set; }
        public ICollection<string> genres { get; set; }
        public ICollection<string> moods { get; set; }
        public ICollection<string> styles { get; set; }
        public string tempo { get; set; }
        public string bpm { get; set; }
        public List<TypeValue> trackTagExtended { get; set; }
        public bool? isTakendown { get; set; }
        public DateTime? takedownDate { get; set; }
        public string smWorkspaceName { get; set; }
        public string smLibraryName { get; set; }
        public string audioUrl { get; set; }

    }

    public class InterestedParties
    {
        public string name { get; set; }
        public string role { get; set; }
        public string labelCode { get; set; }
        public string ipi { get; set; }
        public string isni { get; set; }
        public float? share { get; set; }
    }
   
  
    public class TypeValue
    {
        public int type { get; set; }
        public int value { get; set; }
    }

}
