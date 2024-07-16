using Nest;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{

    public abstract class Document
    {
        public JoinField Join { get; set; }
    }

    public enum TrackType
    {
        standard = 0,
        master = 1,
        matching = 2,
    }
    //public class DHTrackDocument
    //{
    //    public Guid Id { get; set; }
    //    public int? Arid { get; set; }

    //    public ICollection<short> Labels { get; set; }

    //    public TrackData TrackData { get; set; }

    //    public string PreviewUrl { get; set; }

    //    /// <summary>
    //    /// Describes the data source for the track data.
    //    /// </summary>
    //    public Source Source { get; set; }

    //    /// <summary>
    //    /// List of ISO-2 country codes where this metadata applies.
    //    /// </summary>
    //    public ICollection<string> Territories { get; set; }

    //    public TrackType Type { get; set; }

    //    public Guid VersionId { get; set; }

    //    public Guid WorkspaceId { get; set; }

    //    public ELTag AutoTags { get; set; }
    //    public CueCount Cues { get; set; }
    //    public DateTime DateReceived { get; set; }

    //    public List<Asset> Assets { get; set; }
    //    public Guid? libraryId { get; set; }

    //}


    [ElasticsearchType(RelationName = "MLTrackDocument")]
    public class MLTrackDocument
    {
        public Guid id { get; set; }
        public Guid? dhTrackId { get; set; }
        public Guid? oriTrackId { get; set; }
        public Guid? oriVersionId { get; set; }
        [Object(Enabled = false)]
        public IDictionary<string, string> extIdentifiers { get; set; }
        public DateTime dateCreated { get; set; }       
        public DateTime? dateLastEdited { get; set; }
        public int? lastEditedBy { get; set; }
        public long dateReceived { get; set; }
        [Object(Enabled = false)]
        public List<TrackChangeLog> changeLog { get; set; }
        public string copyType { get; set; }
        public ICollection<Asset> assets { get; set; }
        public int? arid { get; set; }
        public string audioFileName { get; set; }
        public Guid wsId { get; set; }
        public string wsName { get; set; }
        public Guid? oriWsId { get; set; }
        public string oriWsName { get; set; }
        public Guid? oriLibId { get; set; }
        public string oriLibName { get; set; }
        public Guid? libId { get; set; }
        public string libName { get; set; }
        public string libLogoFileName { get; set; } //****        
        public string libNotes { get; set; } //****
        public Guid? prodId { get; set; }
        public string prodName { get; set; }
        public string prodSubtitle { get; set; }
        [Object(Enabled = false)]
        public IDictionary<string, string> prodIdentifiers { get; set; }
        public string prodArtworkUrl { get; set; }
        public int? prodYear { get; set; }
        public DateTime? prodRelease { get; set; }
        public string prodNumberOfDiscs { get; set; }
        public string prodDiscNr { get; set; }
        public double? numProdDiscNr { get; set; }
        public double? numProdNumberOfDiscs { get; set; }
        public string prodArtist { get; set; }
        public string prodNotes { get; set; }
        public ICollection<string> prodTags { get; set; }        
        public DateTime? validFrom { get; set; }
        public string releaseIndicator { get; set; }
        public DateTime? validTo { get; set; }
        public ICollection<string> territories { get; set; }
        public string trackTitle { get; set; }
        public string musicOrigin { get; set; }
        public string musicOriginSubIndicator { get; set; }
        public string alternativeTitle { get; set; }
        public string trackVersionTitle { get; set; }
        public float? duration { get; set; }
        public string position { get; set; }
        public double? numPosition { get; set; } // default null - If position is doulble the numPosition = position
        public string subIndex { get; set; }
        public double? numSubIndex { get; set; }
        public string trackNotes { get; set; }
        public Soundmouse.Messaging.Model.Country countryOfFirstRelease { get; set; }
        public bool? retitled { get; set; }

        [Object(Enabled = false)]
        public IDictionary<string, string> trackIdentifiers { get; set; }
        public string lyrics { get; set; }         
        
        [Object(Enabled = false)]
        public List<MLInterestedParty> ips { get; set; }
        public List<string> composer { get; set; }
        public List<string> composer_lyricists { get; set; }
        public List<string> lyricist { get; set; }
        public List<string> adaptor { get; set; }
        public List<string> administrator { get; set; }
        public List<string> arranger { get; set; }
        public List<string> publisher { get; set; }
        public List<string> originalPublisher { get; set; }
        public List<string> performer { get; set; }
        public List<string> recordLabel { get; set; }
        public List<string> subLyricist { get; set; }
        public List<string> subAdaptor { get; set; }
        public List<string> subArranger { get; set; }
        public List<string> subPublisher { get; set; }
        public List<string> translator { get; set; }
        public List<string> composerLyricist { get; set; }        

        public List<string> conFeaturedArtist { get; set; }
        public List<string> conRemixArtist { get; set; }
        public List<string> conVersusArtist { get; set; }
        public List<string> conOrchestra { get; set; }
        public List<string> conConductor { get; set; }
        public List<string> conChoir { get; set; }
        public List<string> conEnsemble { get; set; }

        [Keyword(Normalizer = "ml_normalizer")]
        public string catNo { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string isrc { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string iswc { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string prs { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string upc { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string ean { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string grid { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string barcode { get; set; }
        public List<string> genres { get; set; }
        public ICollection<string> styles { get; set; }
        public ICollection<string> moods { get; set; }
        public ICollection<string> instruments { get; set; }
        public string bpm { get; set; }
        public string tempo { get; set; }
        public double? numBpm { get; set; }
        public double? numTempo { get; set; }
        public ICollection<string> keywords { get; set; }
        public IDictionary<string, string> tags { get; set; }
        [Object(Enabled = false)]
        public List<CTagOrg> cTags { get; set; }  // BBC

        public string wsType { get; set; }
        public bool restricted { get; set; }
        public string source { get; set; }
        public bool? dh_synced { get; set; }
        public bool archived { get; set; }
        public bool sourceDeleted { get; set; }
        public bool? preRelease { get; set; }
        public string searchableType { get; set; }
        public DateTime? searchableFrom { get; set; }
        public int searchableUser { get; set; }
        
        public bool? dhExplicit { get; set; }

        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> allTrackTags { get; set; }   
        public List<string> trackAll { get; set; }

        public string adminNotes { get; set; }  // BBC Admin 
        public string albumAdminNotes { get; set; }  // BBC Admin     
        public List<string> adminTags { get; set; } // BBC Admin 
        public List<string> prodAdminTags { get; set; } // BBC Admin 
        public string bbcTrackId { get; set; }
        public string bbcAlbumId { get; set; }

        public List<string> searchContributorsExtended { get; set; }

        [Object(Enabled = false)]
        public ICollection<Contributor> contributorsExtended { get; set; } // "enabled": false,
        [Object(Enabled = false)]
        public DescriptiveData[] trackDescriptiveExtended { get; set; }
        [Object(Enabled = false)]
        public ICollection<MatchedData> trackMatchedData { get; set; }
        [Object(Enabled = false)]
        public Tag[] trackTagExtended { get; set; }
        [Object(Enabled = false)]
        public TrackGroup trackGroup { get; set; }
        [Object(Enabled = false)]
        public string trackNotesExtended { get; set; }        

        [Object(Enabled = false)]
        public DescriptiveData[] albumDescriptiveExtended { get; set; }
        [Object(Enabled = false)]
        public Tag[] albumTagExtended { get; set; }
        [Object(Enabled = false)]
        public string prodNotesExtended { get; set; }

        public bool? cTag1 { get; set; }
        public bool? cTag2 { get; set; }
        public bool? cTag3 { get; set; }
        public bool? cTag4 { get; set; }
        public bool? cTag5 { get; set; }
        public bool? cTag6 { get; set; }
        public bool? cTag7 { get; set; }
        public bool? cTag8 { get; set; }
        public string org_id { get; set; }
        public bool? prsSearchError { get; set; }
        public bool? prsFound { get; set; }
        public bool? charted { get; set; }
        public bool? chartArtist { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string chartType { get; set; }
        public DateTime? prsSearchDateTime { get; set; }
        public string prsWorkTunecode { get; set; }
        public string prsWorkTitle { get; set; }
        public List<string> prsWorkWriters { get; set; }
        public List<string> prsWorkPublishers { get; set; }
        public List<string> allSearch { get; set; }

        public List<int> indexed_ctags { get; set; }
        public DateTime? indexed_ctag_on { get; set; }
        public Guid? indexed_ctag_idetifier { get; set; }       

        public DateTime? takedownDate { get; set; }
        public int? takedownUser { get; set; }
        public string takedownType { get; set; }

        public bool? mlCreated { get; set; }
        public bool? liveCopy { get; set; }

        public string pLine { get; set; }

        public bool? contentAlert { get; set; }
        public DateTime? dateAlerted { get; set; }
        public int? alertType { get; set; }
        public int? alertedBy { get; set; }
        public string alertNote { get; set; }
        public bool?  alertResolved { get; set; }
        public DateTime? dateAlertResolved { get; set; }
        public int? alertResolvedBy { get; set; }
        public string prodBookletUrl { get; set; }

        public string cTagMcpsOwner { get; set; }
        public string cTagNorthAmerican { get; set; }

        //-- To be removed
        //public DateTime? archivedDateTime { get; set; }
        // public int? archivedBy { get; set; }        
    }

    public class MLInterestedParty
    {
        public string role { get; set; }
        public string fullName { get; set; }
        public IDictionary<string, string> identifiers { get; set; }
        public Soundmouse.Messaging.Model.Society societyAffiliation { get; set; }
        public float? societyShare { get; set; }
    }

    public class CTag
    {
        public int id { get; set; }
        public string name { get; set; }
        public DateTime created { get; set; }
    }

    public class CTagOrg
    {
        public int id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public int? groupId { get; set; }
        public DateTime? created { get; set; }
        public bool? result { get; set; }
    }

    public class ClearanceCTags
    {
        public List<CTagOrg> cTags { get; set; }        
        public DateTime? dateTime { get; set; }
        public string workTunecode { get; set; }
        public string workTitle { get; set; }
        public string workWriters { get; set; }
        public string workPublishers { get; set; }
        public bool isUserEdited { get; set; }
        public bool update { get; set; }
        public bool charted { get; set; }
        public bool prsSearchError { get; set; }
        public bool prsSessionNotFound { get; set; }
        public int prsQueryCount { get; set; }
        public string cTagMcpsOwner { get; set; }
        public string cTagNorthAmerican { get; set; }
        public int? reqestedCtagGroup { get; set; }
    }

    public class PRSFull : ClearanceCTags
    {
        public dynamic prsWork { get; set; }
        public dynamic prsTrack { get; set; }
        public string errorMessage { get; set; }
    }

    public class PRSInfo
    {
        public DateTime dateTime { get; set; }
        public string tunecode { get; set; }
        public string title { get; set; }
        public string artists { get; set; }
    }
}
