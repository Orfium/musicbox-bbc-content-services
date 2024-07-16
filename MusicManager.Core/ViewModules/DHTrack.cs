using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class DHTrack
    {
        public Guid? id { get; set; }
        public Guid? albumId { get; set; }
        public string alternativeTitle { get; set; }        
        public string bpm { get; set; }
        public string discNumber { get; set; }
        public float? duration { get; set; }
        public string filename { get; set; }
        public List<string> genres { get; set; }
        public List<DHValueType> identifiers { get; set; }
        public List<string> instruments { get; set; }
        public List<DHTInterestedParty> interestedParties { get; set; }
        public Guid? libraryId { get; set; }
        public DHTMiscellaneous miscellaneous { get; set; }
        public List<string> moods { get; set; }
        public string musicOrigin { get; set; }
        public string notes { get; set; }
        public string pLine { get; set; }
        public string position { get; set; }
        public bool? publicDomainRecording { get; set; }
        public bool? publicDomainWork { get; set; }
        public bool? retitled { get; set; }
        public bool? seriousMusic { get; set; }
        public bool? stem { get; set; }
        public List<string> styles { get; set; }
        public string subIndex { get; set; }
        public List<string> tags { get; set; }
        public string tempo { get; set; }
        public DHTTerritories territories { get; set; }
        public string title { get; set; }
        public string uniqueId { get; set; }
        public string versionTitle { get; set; }     
        public bool pre_release { get; set; }
        public List<DescriptiveData> descriptiveExtended { get; set; }
        public List<Tag> tagsExtended { get; set; }
        public List<Contributor> contributorsExtended { get; set; }
        public Guid? versionId { get; set; }
        public DHAudio audio { get; set; }
        public DHValidityPeriod validityPeriod { get; set; }
    }

    public class DHAudio
    {
        public long? bitRate { get; set; }
        public DateTime? dateCreated { get; set; }
        public string md5 { get; set; }
        public long? size { get; set; }
        public double? duration { get; set; }
    }


    public class DHValueType
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class DHTInterestedParty
    {
        public string ipi { get; set; }
        public string isni { get; set; }
        public string labelCode { get; set; }
        public string name { get; set; }
        public string role { get; set; }
        public float? share { get; set; }
        public string society { get; set; }
    }

    public class DHTMiscellaneous
    {
        public string sourceRef { get; set; }
        public string chartDbMasterTrackId { get; set; }
        public string dbpowerampTrackId { get; set; }
        public string musicBrainzRecordingId { get; set; }
        public string sourceVersionId { get; set; }
    }

    public class DHTTerritories
    {
        public List<string> include { get; set; }
    }

    public class DHValidityPeriod

    {
        public DateTime? startDate { get; set; }
        public DateTime? endDate { get; set; }       
    }
}
