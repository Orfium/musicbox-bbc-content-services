using MusicManager.Core.ViewModules;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elasticsearch.DataMatching
{
    public static class AlbumDocumentExtention
    {
        public static MLAlbumDocument GenerateMLAlbumDocument(this Product product, MLAlbumDocument mLAlbumDocument, List<MLTrackDocument> mLTrackDocuments, AlbumOrg albumOrg)
        {            
            mLAlbumDocument.id = albumOrg.id;
            mLAlbumDocument.prodId = product.Id;
            mLAlbumDocument.prodName = product.Name;
            mLAlbumDocument.prodSubtitle = product.SubName;            
            mLAlbumDocument.prodIdentifiers = product.Identifiers;
            mLAlbumDocument.prodArtworkUrl = product.ArtworkUri;
            mLAlbumDocument.prodYear = product.Year;
            mLAlbumDocument.prodRelease = product.ReleaseDate?.Date;
            mLAlbumDocument.prodNumberOfDiscs = product.NumberOfDiscs;
            mLAlbumDocument.numProdNumberOfDiscs = product.NumberOfDiscs.StringToDouble();
            mLAlbumDocument.prodArtist = product.Artist;
            mLAlbumDocument.prodNotes = product.Notes;
            mLAlbumDocument.prodTags = product.Tags;
            mLAlbumDocument.cLine = product.CLine;
            mLAlbumDocument.dateCreated = albumOrg.date_created;
            mLAlbumDocument.dateLastEdited = albumOrg.date_last_edited;
            mLAlbumDocument.chartArtist = albumOrg.chart_artist;
            mLAlbumDocument.prodBookletUrl = product.BookletUri;

            if (albumOrg != null) {

                mLAlbumDocument.changeLog = albumOrg.change_log;

                if (albumOrg.chart_info != null)
                {
                    mLAlbumDocument.charted = true;
                    mLAlbumDocument.chartType = albumOrg?.chart_info.chart_type_name;
                }
                else
                {
                    mLAlbumDocument.charted = false;
                }

                mLAlbumDocument.contentAlert = albumOrg.content_alert;
                mLAlbumDocument.alertType = albumOrg.alert_type;
                mLAlbumDocument.alertNote = albumOrg.alert_note;
                mLAlbumDocument.alertedBy = albumOrg.content_alerted_user;
                mLAlbumDocument.dateAlerted = albumOrg.content_alerted_date;
                mLAlbumDocument.alertResolvedBy = albumOrg.ca_resolved_user;
                mLAlbumDocument.dateAlertResolved = albumOrg.ca_resolved_date;
            }            

            if (product.Identifiers != null) {
                mLAlbumDocument.catNo = product.Identifiers.FirstOrDefault(a => a.Key == "catalogue_number").Value;
                mLAlbumDocument.barcode = product.Identifiers.FirstOrDefault(a => a.Key == "barcode").Value;
                mLAlbumDocument.upc = product.Identifiers.FirstOrDefault(a => a.Key == "upc").Value;
                mLAlbumDocument.ean = product.Identifiers.FirstOrDefault(a => a.Key == "ean").Value;
                mLAlbumDocument.grid = product.Identifiers.FirstOrDefault(a => a.Key == "grid").Value;
            }            
            mLAlbumDocument.trackCount = mLTrackDocuments!=null ? mLTrackDocuments.Count : 0;

            if (product.DescriptiveExtended != null)
            {
                try
                {                   
                    DescriptiveData bbc_admin_notes = product.DescriptiveExtended?.SingleOrDefault(a => a.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString());
                    DescriptiveData bbc_album_id = product.DescriptiveExtended?.SingleOrDefault(a => a.Type == enDescriptiveExtendedType.bbc_album_id.ToString());
                                        

                    if (bbc_admin_notes != null)
                    {
                        mLAlbumDocument.adminNotes = bbc_admin_notes.Value.ToString();
                    }

                    if (bbc_album_id != null)
                    {
                        mLAlbumDocument.bbcAlbumId = bbc_album_id.Value.ToString();
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }           

            //--- Map prod BBC Admin tags
            if (product.TagsExtended?.Count() > 0)
            {
                mLAlbumDocument.adminTags = new List<string>();
                foreach (var item in product.TagsExtended)
                {
                    if (item.Type == enAdminTypes.BBC_ADMIN_TAG.ToString()) {
                        mLAlbumDocument.adminTags.Add(item.Value);
                    }                    
                }
                mLAlbumDocument.adminTags = mLAlbumDocument.adminTags.Distinct().ToList();
            }

            if (mLAlbumDocument.trackCount > 0) {
                KeyValuePair<string, string> orignSubOrigin = TrackDocumentExtensions.CleanseOrigin(mLTrackDocuments[0].musicOrigin);   

                mLAlbumDocument.musicOrigin = orignSubOrigin.Key;
                mLAlbumDocument.musicOriginSubIndicator = orignSubOrigin.Value;
                mLAlbumDocument.territories = mLTrackDocuments[0].territories;
                mLAlbumDocument.ipRecordLabel = mLTrackDocuments[0].recordLabel;
                mLAlbumDocument = MapTrackData(mLAlbumDocument, mLTrackDocuments);
            }                

            mLAlbumDocument.allSearch = new List<string>();
            mLAlbumDocument.allSearch.Add(mLAlbumDocument.prodName);
            if (!string.IsNullOrEmpty(mLAlbumDocument.prodArtist))
                mLAlbumDocument.allSearch.Add(mLAlbumDocument.prodArtist);

           

            return mLAlbumDocument;
        }

        private static MLAlbumDocument MapTrackData(MLAlbumDocument mLAlbumDocument, List<MLTrackDocument> mLTrackDocuments)
        {
            mLAlbumDocument.trackTitle = new List<string>();
            mLAlbumDocument.trackIsrc = new List<string>();
            mLAlbumDocument.trackIswc = new List<string>();
            mLAlbumDocument.trackPrs = new List<string>();
            mLAlbumDocument.trackComposer = new List<string>();
            mLAlbumDocument.trackLyricist = new List<string>();
            mLAlbumDocument.trackAdaptor = new List<string>();
            mLAlbumDocument.trackAdministrator = new List<string>();
            mLAlbumDocument.trackArranger = new List<string>();
            mLAlbumDocument.trackPublisher = new List<string>();
            mLAlbumDocument.trackOriginalPublisher = new List<string>();
            mLAlbumDocument.trackPerformer = new List<string>();
            mLAlbumDocument.trackRecordLabel = new List<string>();
            mLAlbumDocument.trackSubLyricist = new List<string>();
            mLAlbumDocument.trackSubAdaptor = new List<string>();
            mLAlbumDocument.trackSubArranger = new List<string>();
            mLAlbumDocument.trackSubPublisher = new List<string>();
            mLAlbumDocument.trackTranslator = new List<string>();
            mLAlbumDocument.trackComposerLyricist = new List<string>();

            mLAlbumDocument.trackBpm = new List<string>();
            mLAlbumDocument.trackTempo = new List<string>();

            mLAlbumDocument.trackGenres = new List<string>();
            mLAlbumDocument.trackStyles = new List<string>();
            mLAlbumDocument.trackMoods = new List<string>();
            mLAlbumDocument.trackInstruments = new List<string>();
            mLAlbumDocument.trackKeywords = new List<string>();

            mLAlbumDocument.allAlbumTags = new List<string>();
            mLAlbumDocument.albumAll = new List<string>();

            foreach (MLTrackDocument item in mLTrackDocuments)
            {
                if (!string.IsNullOrEmpty(item.trackTitle))
                    mLAlbumDocument.trackTitle.Add(item.trackTitle);
                if (!string.IsNullOrEmpty(item.isrc))
                    mLAlbumDocument.trackIsrc.Add(item.isrc);
                if (!string.IsNullOrEmpty(item.iswc))
                    mLAlbumDocument.trackIswc.Add(item.iswc);
                if (!string.IsNullOrEmpty(item.prs))
                    mLAlbumDocument.trackPrs.Add(item.prs);
                if (!string.IsNullOrEmpty(item.bpm))
                    mLAlbumDocument.trackBpm.Add(item.bpm);
                if (!string.IsNullOrEmpty(item.tempo))
                    mLAlbumDocument.trackTempo.Add(item.tempo);
                if (item.composer!=null)                    
                    mLAlbumDocument.trackComposer.AddRange(item.composer);
                if (item.lyricist != null)
                    mLAlbumDocument.trackLyricist.AddRange(item.lyricist);
                if (item.adaptor != null)
                    mLAlbumDocument.trackAdaptor.AddRange(item.adaptor);
                if (item.administrator != null)
                    mLAlbumDocument.trackAdministrator.AddRange(item.administrator);
                if (item.arranger != null)
                    mLAlbumDocument.trackArranger.AddRange(item.arranger);
                if (item.publisher != null)
                    mLAlbumDocument.trackPublisher.AddRange(item.publisher);
                if (item.originalPublisher != null)
                    mLAlbumDocument.trackOriginalPublisher.AddRange(item.originalPublisher);
                if (item.performer != null)
                    mLAlbumDocument.trackPerformer.AddRange(item.performer);
                if (item.recordLabel != null)
                    mLAlbumDocument.trackRecordLabel.AddRange(item.recordLabel);
                if (item.subLyricist != null)
                    mLAlbumDocument.trackSubLyricist.AddRange(item.subLyricist);
                if (item.subAdaptor != null)
                    mLAlbumDocument.trackSubAdaptor.AddRange(item.subAdaptor);
                if (item.subArranger != null)
                    mLAlbumDocument.trackSubArranger.AddRange(item.subArranger);
                if (item.subPublisher != null)
                    mLAlbumDocument.trackSubPublisher.AddRange(item.subPublisher);
                if (item.translator != null)
                    mLAlbumDocument.trackTranslator.AddRange(item.translator);
                if (item.composerLyricist != null)
                    mLAlbumDocument.trackComposerLyricist.AddRange(item.composerLyricist);
                if (item.genres?.Count() >0)
                    mLAlbumDocument.trackGenres.AddRange(item.genres);
                if (item.styles?.Count() > 0)
                    mLAlbumDocument.trackStyles.AddRange(item.styles.ToList());
                if (item.moods?.Count() > 0)
                    mLAlbumDocument.trackMoods.AddRange(item.moods.ToList());
                if (item.instruments?.Count() > 0)
                    mLAlbumDocument.trackInstruments.AddRange(item.instruments.ToList());
                if (item.keywords?.Count() > 0)
                    mLAlbumDocument.trackKeywords.AddRange(item.keywords.ToList());
            }

            mLAlbumDocument.trackGenres = TrackDocumentExtensions.CleanseTags(mLAlbumDocument.trackGenres);
            mLAlbumDocument.trackStyles = TrackDocumentExtensions.CleanseTags(mLAlbumDocument.trackStyles);
            mLAlbumDocument.trackMoods = TrackDocumentExtensions.CleanseTags(mLAlbumDocument.trackMoods);
            mLAlbumDocument.trackInstruments = TrackDocumentExtensions.CleanseTags(mLAlbumDocument.trackInstruments);
            mLAlbumDocument.trackKeywords = TrackDocumentExtensions.CleanseTags(mLAlbumDocument.trackKeywords);

            if (mLAlbumDocument.trackGenres?.Count() > 0) {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.trackGenres);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackGenres);
            }
            if (mLAlbumDocument.trackStyles?.Count() > 0) {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.trackStyles);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackStyles);
            }
               
            if (mLAlbumDocument.trackMoods?.Count() > 0) {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.trackMoods);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackMoods);
            }
               
            if (mLAlbumDocument.trackInstruments?.Count() > 0) {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.trackInstruments);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackInstruments);
            }
                
            if (mLAlbumDocument.trackKeywords?.Count() > 0) {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.trackKeywords);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackKeywords);
            }

            if (mLAlbumDocument.trackBpm?.Count() > 0)
            {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.trackBpm);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackBpm);
            }

            if (mLAlbumDocument.trackTempo?.Count() > 0)
            {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.trackTempo);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackTempo);
            }

            if (mLAlbumDocument.prodTags?.Count > 0)
            {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.prodTags);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.prodTags);
            }

            if (mLAlbumDocument.adminTags?.Count > 0)
            {
                mLAlbumDocument.allAlbumTags.AddRange(mLAlbumDocument.adminTags);
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.adminTags);
            }

            if (!string.IsNullOrWhiteSpace(mLAlbumDocument.prodArtist)) {
                mLAlbumDocument.albumAll.Add(mLAlbumDocument.prodArtist);
            }

            mLAlbumDocument.trackTitle = mLAlbumDocument.trackTitle.Distinct().ToList();
            mLAlbumDocument.trackIsrc = mLAlbumDocument.trackIsrc.Distinct().ToList();
            mLAlbumDocument.trackIswc = mLAlbumDocument.trackIswc.Distinct().ToList();
            mLAlbumDocument.trackPrs = mLAlbumDocument.trackPrs.Distinct().ToList();
            mLAlbumDocument.trackComposer = mLAlbumDocument.trackComposer.Distinct().ToList();
            mLAlbumDocument.trackLyricist = mLAlbumDocument.trackLyricist.Distinct().ToList();
            mLAlbumDocument.trackAdaptor = mLAlbumDocument.trackAdaptor.Distinct().ToList();
            mLAlbumDocument.trackAdministrator = mLAlbumDocument.trackAdministrator.Distinct().ToList();
            mLAlbumDocument.trackArranger = mLAlbumDocument.trackArranger.Distinct().ToList();
            mLAlbumDocument.trackPublisher = mLAlbumDocument.trackPublisher.Distinct().ToList();
            mLAlbumDocument.trackOriginalPublisher = mLAlbumDocument.trackOriginalPublisher.Distinct().ToList();
            mLAlbumDocument.trackPerformer = mLAlbumDocument.trackPerformer.Distinct().ToList();
            mLAlbumDocument.trackRecordLabel = mLAlbumDocument.trackRecordLabel.Distinct().ToList();
            mLAlbumDocument.trackSubLyricist = mLAlbumDocument.trackSubLyricist.Distinct().ToList();
            mLAlbumDocument.trackSubAdaptor = mLAlbumDocument.trackSubAdaptor.Distinct().ToList();
            mLAlbumDocument.trackSubArranger = mLAlbumDocument.trackSubArranger.Distinct().ToList();
            mLAlbumDocument.trackSubPublisher = mLAlbumDocument.trackSubPublisher.Distinct().ToList();
            mLAlbumDocument.trackTranslator = mLAlbumDocument.trackTranslator.Distinct().ToList();
            mLAlbumDocument.trackBpm = mLAlbumDocument.trackBpm.Distinct().ToList();
            mLAlbumDocument.trackTempo = mLAlbumDocument.trackTempo.Distinct().ToList();

            if (mLAlbumDocument.trackTitle.Count()>0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackTitle);
            if (mLAlbumDocument.trackComposer.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackComposer);
            if (mLAlbumDocument.trackLyricist.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackLyricist);
            if (mLAlbumDocument.trackAdaptor.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackAdaptor);
            if (mLAlbumDocument.trackAdministrator.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackAdministrator);
            if (mLAlbumDocument.trackArranger.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackArranger);
            if (mLAlbumDocument.trackPublisher.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackPublisher);
            if (mLAlbumDocument.trackOriginalPublisher.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackOriginalPublisher);
            if (mLAlbumDocument.trackPerformer.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackPerformer);
            if (mLAlbumDocument.trackRecordLabel.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackRecordLabel);
            if (mLAlbumDocument.trackSubLyricist.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackSubLyricist);
            if (mLAlbumDocument.trackSubAdaptor.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackSubAdaptor);
            if (mLAlbumDocument.trackSubArranger.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackSubArranger);
            if (mLAlbumDocument.trackSubPublisher.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackSubPublisher);
            if (mLAlbumDocument.trackTranslator.Count() > 0)
                mLAlbumDocument.albumAll.AddRange(mLAlbumDocument.trackTranslator);

            mLAlbumDocument.allAlbumTags = mLAlbumDocument.allAlbumTags.Distinct().ToList();
            mLAlbumDocument.albumAll = mLAlbumDocument.albumAll.Distinct().ToList();

            return mLAlbumDocument;
        }

        public static List<string> GetAllTrackTags(MLTrackDocument mLTrackDocument)
        {
            List<string> trackTags = new List<string>();

            if (mLTrackDocument.genres?.Count() > 0)
                trackTags.AddRange(mLTrackDocument.genres);

            if (mLTrackDocument.styles?.Count() > 0)
                trackTags.AddRange(mLTrackDocument.styles);

            if (mLTrackDocument.moods?.Count() > 0)
                trackTags.AddRange(mLTrackDocument.moods);

            if (mLTrackDocument.instruments?.Count() > 0)
                trackTags.AddRange(mLTrackDocument.instruments);

            if (mLTrackDocument.keywords?.Count() > 0)
                trackTags.AddRange(mLTrackDocument.keywords);

            trackTags = trackTags.Distinct().ToList();

            return trackTags;
        }

    }
}
