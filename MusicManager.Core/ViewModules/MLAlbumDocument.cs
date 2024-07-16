using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    [ElasticsearchType(RelationName = "MLAlbumDocument")]
    public class MLAlbumDocument
    {
        public Guid? id { get; set; }
        public Guid? prodId { get; set; }
        public Guid? wsId { get; set; }
        public string wsName { get; set; }
        public string wsType { get; set; }
        public Guid? oriWsId { get; set; }
        public string oriWsName { get; set; }
        public Guid? libId { get; set; }
        public string libName { get; set; }
        public Guid? otiLibId { get; set; }
        public string oriLibName { get; set; }
        public string libLogoFileName { get; set; }
        public string libNotes { get; set; }
        public string prodName { get; set; }
        public string prodSubtitle { get; set; }
        public bool sourceDeleted { get; set; }
        public bool restricted { get; set; }       
        public bool archived { get; set; }
        public DateTime dateCreated { get; set; }        
        public DateTime? dateLastEdited { get; set; }

        [Object(Enabled = false)]
        public IDictionary<string, string> prodIdentifiers { get; set; }
        public string prodArtworkUrl { get; set; }
        public int? prodYear { get; set; }
        public DateTime? prodRelease { get; set; }
        public string prodNumberOfDiscs { get; set; }
        public double? numProdNumberOfDiscs { get; set; }
        public string prodDiscNr { get; set; }
        public string prodArtist { get; set; }
        public string prodNotes { get; set; }
        public string musicOrigin { get; set; }
        public string musicOriginSubIndicator { get; set; }
        public ICollection<string> territories { get; set; }
        public List<string> ipRecordLabel { get; set; }        
        public ICollection<string> prodTags { get; set; }
       
        public string adminNotes { get; set; }  // BBC Admin             
        public List<string> adminTags { get; set; } // BBC Admin
        public string bbcAlbumId { get; set; }

        [Keyword(Normalizer = "ml_normalizer")]
        public string catNo { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string barcode { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string upc { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string ean { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string grid { get; set; }        
        public List<int> cTags { get; set; }  // BBC

        public int trackCount { get; set; }
        public List<string> trackTitle { get; set; }
        public List<string> trackComposer { get; set; }
        public List<string> trackLyricist { get; set; }
        public List<string> trackAdaptor { get; set; }
        public List<string> trackAdministrator { get; set; }
        public List<string> trackArranger { get; set; }
        public List<string> trackPublisher { get; set; }
        public List<string> trackOriginalPublisher { get; set; }
        public List<string> trackPerformer { get; set; }
        public List<string> trackRecordLabel { get; set; }
        public List<string> trackSubLyricist { get; set; }
        public List<string> trackSubAdaptor { get; set; }
        public List<string> trackSubArranger { get; set; }
        public List<string> trackSubPublisher { get; set; }
        public List<string> trackTranslator { get; set; }
        public List<string> trackComposerLyricist { get; set; }
        public List<string> trackIsrc { get; set; }
        public List<string> trackIswc { get; set; }
        public List<string> trackPrs { get; set; }
        public List<string> trackBpm { get; set; }
        public List<string> trackTempo { get; set; }

        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> trackGenres { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> trackStyles { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> trackMoods { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> trackInstruments { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> trackKeywords { get; set; }  
        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> albumAll { get; set; } //
        [Keyword(Normalizer = "ml_normalizer")]
        public List<string> allAlbumTags { get; set; } // All track IPs / ProdTags ()
        public List<TrackChangeLog> changeLog { get; set; }
        public List<string> allSearch { get; set; }

        public bool? charted { get; set; }
        [Keyword(Normalizer = "ml_normalizer")]
        public string chartType { get; set; }
        public bool? chartArtist { get; set; }
        public string cLine { get; set; }

        public bool? contentAlert { get; set; }
        public DateTime? dateAlerted { get; set; }
        public int? alertType { get; set; }
        public int? alertedBy { get; set; }
        public string alertNote { get; set; }
        public bool? alertResolved { get; set; }
        public DateTime? dateAlertResolved { get; set; }
        public int? alertResolvedBy { get; set; }
        public string prodBookletUrl { get; set; }        
    }
}
