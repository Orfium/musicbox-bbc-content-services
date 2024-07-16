using Elasticsearch.Util;
using MusicManager.Core.ViewModules;
using Nest;
using Newtonsoft.Json;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Elasticsearch.DataMatching
{
    public static class TrackDocumentExtensions
    {
        public static MLTrackDocument GenerateMLTrackDocument(this Track dhTrackDocument, LogElasticTrackChange logElasticTrackChange, MLTrackDocument mlTrackDocument, TrackOrg trackOrg)
        {
            try
            {
                mlTrackDocument.dh_synced = true;
                mlTrackDocument.dhTrackId = dhTrackDocument.Id; // Datahub Track ID    
                mlTrackDocument.allSearch = new List<string>();

                if (logElasticTrackChange != null && logElasticTrackChange.source_ref == "ML_TRACK_ADD")
                {
                    mlTrackDocument.source = "ml";
                }
                else
                {
                    mlTrackDocument.source = "dh";
                }

                mlTrackDocument.assets = dhTrackDocument.Assets;
                mlTrackDocument.arid = dhTrackDocument.Arid;
                mlTrackDocument.wsId = dhTrackDocument.WorkspaceId;

                if (dhTrackDocument.TrackData.Product != null)
                {                    
                    mlTrackDocument.albumTagExtended = dhTrackDocument.TrackData.Product.TagsExtended;

                    mlTrackDocument.extIdentifiers = dhTrackDocument.TrackData.Identifiers;
                    mlTrackDocument.prodId = dhTrackDocument.TrackData.Product.Id;
                    mlTrackDocument.prodName = dhTrackDocument.TrackData.Product.Name.ReplaceSpecialCodes();
                    mlTrackDocument.allSearch.Add(mlTrackDocument.prodName);
                    mlTrackDocument.prodSubtitle = dhTrackDocument.TrackData.Product.SubName.ReplaceSpecialCodes();
                    mlTrackDocument.prodIdentifiers = dhTrackDocument.TrackData.Product.Identifiers;
                    mlTrackDocument.prodArtworkUrl = dhTrackDocument.TrackData.Product.ArtworkUri;
                    mlTrackDocument.prodYear = dhTrackDocument.TrackData.Product.Year;
                    mlTrackDocument.prodRelease = dhTrackDocument.TrackData.Product.ReleaseDate;
                    mlTrackDocument.prodNumberOfDiscs = dhTrackDocument.TrackData.Product.NumberOfDiscs;
                    mlTrackDocument.numProdNumberOfDiscs = dhTrackDocument.TrackData.Product.NumberOfDiscs.StringToDouble(); 

                    //--- Avoid invalid disk numbers
                    if (dhTrackDocument.TrackData.DiscNumber?.Length < 5)
                    {
                        mlTrackDocument.numProdDiscNr = dhTrackDocument.TrackData.DiscNumber.StringToDouble();
                        mlTrackDocument.prodDiscNr = dhTrackDocument.TrackData.DiscNumber;
                    }
                    else {
                        mlTrackDocument.numProdDiscNr = 0;
                    }                      

                    mlTrackDocument.prodArtist = dhTrackDocument.TrackData.Product.Artist.ReplaceSpecialCodes();
                    mlTrackDocument.prodNotes = dhTrackDocument.TrackData.Product.Notes.ReplaceSpecialCodes();
                    mlTrackDocument.prodBookletUrl = dhTrackDocument.TrackData.Product.BookletUri;

                    if (mlTrackDocument.prodIdentifiers != null && mlTrackDocument.prodIdentifiers.Count > 0)
                    {
                        mlTrackDocument.catNo = mlTrackDocument.prodIdentifiers.FirstOrDefault(a => a.Key == "catalogue_number").Value;
                        mlTrackDocument.upc = mlTrackDocument.prodIdentifiers.FirstOrDefault(a => a.Key == "upc").Value;
                        mlTrackDocument.ean = mlTrackDocument.prodIdentifiers.FirstOrDefault(a => a.Key == "ean").Value;
                        mlTrackDocument.grid = mlTrackDocument.prodIdentifiers.FirstOrDefault(a => a.Key == "grid").Value;
                        mlTrackDocument.barcode = mlTrackDocument.prodIdentifiers.FirstOrDefault(a => a.Key == "barcode").Value;
                    }                  

                    if (dhTrackDocument.TrackData.Product.DescriptiveExtended != null)
                    {
                        try
                        {
                            DescriptiveData bbc_admin_notes = dhTrackDocument.TrackData.Product.DescriptiveExtended?.SingleOrDefault(a => a.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString());
                            DescriptiveData bbc_album_id = dhTrackDocument.TrackData.Product.DescriptiveExtended?.SingleOrDefault(a => a.Type == enDescriptiveExtendedType.bbc_album_id.ToString());


                            if (bbc_admin_notes != null)
                            {
                                mlTrackDocument.albumAdminNotes = bbc_admin_notes.Value.ToString();
                            }

                            if (bbc_album_id != null)
                            {
                                mlTrackDocument.bbcAlbumId = bbc_album_id.Value.ToString();
                            }
                        }
                        catch (Exception)
                        {
                            
                        }
                    }                  

                    mlTrackDocument.prodTags = CleanseTags(dhTrackDocument.TrackData.Product.Tags?.ToList());
                }
                if (dhTrackDocument.Source != null)
                {  
                    mlTrackDocument.validFrom = dhTrackDocument.Source.ValidFrom;
                    mlTrackDocument.validTo = dhTrackDocument.Source.ValidTo;
                }

                if (dhTrackDocument.TrackData != null)
                {
                    mlTrackDocument.pLine = dhTrackDocument.TrackData.PLine;
                    mlTrackDocument.dhExplicit = dhTrackDocument.TrackData.Explicit;

                    mlTrackDocument.contributorsExtended = dhTrackDocument.TrackData.ContributorsExtended;

                    if (mlTrackDocument.contributorsExtended?.Count() > 0)
                    {
                        mlTrackDocument.searchContributorsExtended = new List<string>();
                        foreach (var item in mlTrackDocument.contributorsExtended)
                        {
                            mlTrackDocument.searchContributorsExtended.Add(item.Role?.Replace("_"," ").ToUpper() + ": " + item.Name);
                        }
                    }

                    if (dhTrackDocument.TrackData.DescriptiveExtended != null) {
                        try
                        {
                            DescriptiveData ML_UPLOAD = dhTrackDocument.TrackData.DescriptiveExtended.SingleOrDefault(a => a.Source == enDescriptiveExtendedSource.ML_UPLOAD.ToString());
                            DescriptiveData bbc_admin_notes = dhTrackDocument.TrackData.DescriptiveExtended.SingleOrDefault(a => a.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString());
                            DescriptiveData bbc_track_id = dhTrackDocument.TrackData.DescriptiveExtended.SingleOrDefault(a => a.Type == enDescriptiveExtendedType.bbc_track_id.ToString());

                            if (ML_UPLOAD != null) {
                                string assetS3Id = ML_UPLOAD.Value.GetValueFromObject("AssetS3Id");
                                string size = ML_UPLOAD.Value.GetValueFromObject("Size");
                                if (!string.IsNullOrEmpty(assetS3Id))
                                {

                                    if (mlTrackDocument.assets == null)
                                        mlTrackDocument.assets = new List<Asset>();

                                    mlTrackDocument.assets.Add(new Asset()
                                    {
                                        Key = assetS3Id,
                                        BucketName = ML_UPLOAD.Value.GetValueFromObject("BucketName"),
                                        Type = Path.GetExtension(assetS3Id).TrimStart('.'),
                                        Quality = 0,
                                        Size = -1
                                    });
                                }
                            }

                            if (bbc_admin_notes !=null)
                            {
                                mlTrackDocument.adminNotes = bbc_admin_notes.Value.ToString();
                            }

                            if (bbc_track_id !=null)
                            {
                                mlTrackDocument.bbcTrackId = bbc_track_id.Value.ToString();
                            }

                        }
                        catch (Exception)
                        {
                            throw;
                        }                       
                    }                    

                    mlTrackDocument.trackMatchedData = dhTrackDocument.TrackData.MatchedData;                    

                    //--- Map BBC Fields
                    if (dhTrackDocument.TrackData?.TagsExtended?.Count() > 0) {
                        mlTrackDocument.trackTagExtended = dhTrackDocument.TrackData.TagsExtended;
                        mlTrackDocument.adminTags = new List<string>();
                        foreach (var item in dhTrackDocument.TrackData?.TagsExtended)
                        {
                            if (item.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
                            {
                                mlTrackDocument.adminTags.Add(item.Value);
                            }                           
                        }
                    }

                    KeyValuePair<string, string> orignSubOrigin = CleanseOrigin(dhTrackDocument.TrackData?.MusicOrigin);
                    mlTrackDocument.musicOrigin = orignSubOrigin.Key;
                    mlTrackDocument.musicOriginSubIndicator = orignSubOrigin.Value;

                    mlTrackDocument.trackGroup = dhTrackDocument.TrackData.TrackGroup;
                    mlTrackDocument.audioFileName = dhTrackDocument.TrackData.FileName;
                    mlTrackDocument.libId = dhTrackDocument.TrackData.LibraryId;
                    mlTrackDocument.trackTitle = dhTrackDocument.TrackData.Title.ReplaceSpecialCodes();
                    mlTrackDocument.allSearch.Add(mlTrackDocument.trackTitle);                  
                    mlTrackDocument.alternativeTitle = dhTrackDocument.TrackData.AlternativeTitle.ReplaceSpecialCodes();
                    mlTrackDocument.trackVersionTitle = dhTrackDocument.TrackData.VersionTitle.ReplaceSpecialCodes();
                    mlTrackDocument.duration = dhTrackDocument.TrackData.Duration;
                    
                    //--- Avoid invalid disk numbers
                    if (dhTrackDocument.TrackData.Position?.Length < 5)
                    {
                        mlTrackDocument.numPosition = dhTrackDocument.TrackData.Position.StringToDouble();
                        mlTrackDocument.position = dhTrackDocument.TrackData.Position;
                    }
                    else {
                        mlTrackDocument.numPosition = 0;
                    }                        

                    mlTrackDocument.subIndex = dhTrackDocument.TrackData.SubIndex;
                    mlTrackDocument.numSubIndex = dhTrackDocument.TrackData.SubIndex.StringToDouble();
                    mlTrackDocument.trackNotes = dhTrackDocument.TrackData.Notes;
                    mlTrackDocument.countryOfFirstRelease = dhTrackDocument.TrackData.CountryOfFirstRelease;
                    mlTrackDocument.retitled = dhTrackDocument.TrackData.Retitled;
                    mlTrackDocument.trackIdentifiers = dhTrackDocument.TrackData.Identifiers;

                    if (mlTrackDocument.trackIdentifiers != null && mlTrackDocument.trackIdentifiers.Count > 0)
                    {
                        mlTrackDocument.isrc = mlTrackDocument.trackIdentifiers.FirstOrDefault(a => a.Key == "isrc").Value;
                        mlTrackDocument.iswc = mlTrackDocument.trackIdentifiers.FirstOrDefault(a => a.Key == "iswc").Value;
                        mlTrackDocument.prs = mlTrackDocument.trackIdentifiers.FirstOrDefault(a => a.Key == "prs").Value;
                    }

                    if (dhTrackDocument.TrackData.InterestedParties != null && dhTrackDocument.TrackData.InterestedParties.Count > 0)
                    {
                        mlTrackDocument.ips = new List<MLInterestedParty>();
                        mlTrackDocument.ips.AddRange(MapIpList(dhTrackDocument.TrackData.InterestedParties));
                       
                        mlTrackDocument.composer = GetNameListByRole("composer", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.lyricist = GetNameListByRole("lyricist", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.adaptor = GetNameListByRole("adaptor", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.administrator = GetNameListByRole("administrator", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.arranger = GetNameListByRole("arranger", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.publisher = GetNameListByRole("publisher", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.originalPublisher = GetNameListByRole("original_publisher", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.performer = GetNameListByRole("performer", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.recordLabel = GetNameListByRole("record_label", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.subLyricist = GetNameListByRole("sub_lyricist", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.subAdaptor = GetNameListByRole("sub_adaptor", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.subArranger = GetNameListByRole("sub_arranger", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.subPublisher = GetNameListByRole("sub_publisher", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.translator = GetNameListByRole("translator", dhTrackDocument.TrackData.InterestedParties);
                        mlTrackDocument.composerLyricist = GetNameListByRole("composer_lyricist", dhTrackDocument.TrackData.InterestedParties);

                        if (mlTrackDocument.performer?.Count() > 0)
                            mlTrackDocument.allSearch.AddRange(mlTrackDocument.performer);                       
                    }
                    mlTrackDocument.moods = CleanseTags(dhTrackDocument.TrackData.Moods?.ToList());
                    mlTrackDocument.genres = CleanseTags(dhTrackDocument.TrackData.Genres?.ToList());
                    mlTrackDocument.styles = CleanseTags(dhTrackDocument.TrackData.Styles?.ToList());
                    mlTrackDocument.instruments = CleanseTags(dhTrackDocument.TrackData.Instrumentations?.ToList());
                    mlTrackDocument.bpm = dhTrackDocument.TrackData.Bpm;
                    mlTrackDocument.numBpm = dhTrackDocument.TrackData.Bpm.StringToDouble();
                    mlTrackDocument.tempo = dhTrackDocument.TrackData.Tempo;
                    mlTrackDocument.numTempo = dhTrackDocument.TrackData.Tempo.StringToDouble();
                    mlTrackDocument.keywords = CleanseTags(dhTrackDocument.TrackData.Keywords?.ToList());  //-- Updated tags returns as keywords                    

                }

                mlTrackDocument.territories = dhTrackDocument.Territories;

                if (trackOrg != null)
                {
                    mlTrackDocument.id = trackOrg.id; // ML Track ID                     
                    //-- BBC Specific ---- COMENT ----------------------------------------------
                    //mlTrackDocument.preReleaseDate = trackOrg.pre_release_date;
                    //mlTrackDocument.preReleaseIndicator = trackOrg.pre_release_indicator;
                    //mlTrackDocument.releaseIndicator = trackOrg.release_indicator;
                    //mlTrackDocument.lyrics = trackOrg.lyrics;  
                    //mlTrackDocument.bbcAdminNotes = trackOrg.bbc_admin_notes;  
                    //mlTrackDocument.bbcLicenceNotes = trackOrg.bbc_licence_notes;   
                    //mlTrackDocument.bbcLibrarianNotes = trackOrg.bbc_librarian_notes; 
                    //----------------------------------------------------------------------------

                    mlTrackDocument.contentAlert = trackOrg.content_alert;
                    mlTrackDocument.alertType = trackOrg.alert_type;
                    mlTrackDocument.alertNote = trackOrg.alert_note;
                    mlTrackDocument.alertedBy = trackOrg.content_alerted_user;
                    mlTrackDocument.dateAlerted = trackOrg.content_alerted_date;
                    mlTrackDocument.alertResolvedBy = trackOrg.ca_resolved_user;
                    mlTrackDocument.dateAlertResolved = trackOrg.ca_resolved_date;

                    mlTrackDocument.dateCreated = trackOrg.date_created;
                    mlTrackDocument.dateLastEdited = trackOrg.date_last_edited;
                    mlTrackDocument.lastEditedBy = trackOrg.last_edited_by;
                    mlTrackDocument.changeLog = trackOrg.change_log;
                    mlTrackDocument.restricted = trackOrg.restricted ?? false;
                    mlTrackDocument.cTags = new List<CTagOrg>();
                    mlTrackDocument.chartArtist = trackOrg.chart_artist;

                    Tag orgSubOrigin = trackOrg.org_data?.SingleOrDefault(a=>a.Type == enAdminTypes.SUB_ORIGIN.ToString());

                    if (orgSubOrigin != null) {
                        switch (orgSubOrigin.Value)
                        {
                            case "2":
                                mlTrackDocument.musicOriginSubIndicator = "MCPS";
                                break;

                            case "3":
                                mlTrackDocument.musicOriginSubIndicator = "Non-MCPS";
                                break;
                        }
                    }

                    if (trackOrg.c_tags !=null )
                    {
                        mlTrackDocument.cTags = trackOrg.c_tags?.cTags?.Where(a => a.result != null).ToList();  // BBC
                        mlTrackDocument.prsFound = true;                        
                        mlTrackDocument.prsSearchDateTime = trackOrg.c_tags.dateTime;
                        mlTrackDocument.prsWorkTunecode = trackOrg.c_tags.workTunecode;
                        mlTrackDocument.prsWorkTitle = trackOrg.c_tags.workTitle;
                        mlTrackDocument.prsWorkWriters = trackOrg.c_tags.workWriters.SplitByComma();
                        mlTrackDocument.prsWorkPublishers = trackOrg.c_tags.workPublishers.SplitByComma();
                        mlTrackDocument.cTagMcpsOwner = GetCtagStatus(trackOrg.c_tags, (int)enCTagTypes.PRS_MCPS_OWNERSHIP);
                        mlTrackDocument.cTagNorthAmerican = GetCtagStatus(trackOrg.c_tags, (int)enCTagTypes.NORTH_AMERICAN_COPYRIGHT);
                    }

                    if (trackOrg.chart_info != null)
                    {
                        mlTrackDocument.charted = true;
                        mlTrackDocument.chartType = trackOrg?.chart_info.chart_type_name;
                    }
                    else {
                        mlTrackDocument.charted = false;
                    }
                } 

                mlTrackDocument.allTrackTags = GetAllTrackTags(mlTrackDocument);
                mlTrackDocument.trackAll = GetTrackAll(mlTrackDocument);

                return mlTrackDocument;
            }
            catch (Exception)
            {              
                throw;
            }            
        }

        public static Track MLTrackDocumentToDHTrack(this MLTrackDocument mlTrackDocument)
        {
            List<InterestedParty> interestedParties = new List<InterestedParty>();

            Track track = new Track()
            {
                Id = mlTrackDocument.dhTrackId == null || mlTrackDocument.dhTrackId == Guid.Empty ? Guid.NewGuid() : (Guid)mlTrackDocument.dhTrackId,
                TrackData = new TrackData()
                {                    
                    Identifiers = new Dictionary<string, string>(),
                    Title = mlTrackDocument.trackTitle,
                    Bpm = string.IsNullOrEmpty(mlTrackDocument.bpm) ? null : mlTrackDocument.bpm,
                    AlternativeTitle = string.IsNullOrEmpty(mlTrackDocument.alternativeTitle) ? null : mlTrackDocument.alternativeTitle,
                    DiscNumber = string.IsNullOrEmpty(mlTrackDocument.prodDiscNr) ? null : mlTrackDocument.prodDiscNr,
                    MusicOrigin = mlTrackDocument.musicOrigin,
                    FileName = string.IsNullOrEmpty(mlTrackDocument.audioFileName) ? null : mlTrackDocument.audioFileName,
                    Duration = mlTrackDocument.duration,
                    Position = string.IsNullOrEmpty(mlTrackDocument.position) ? null : mlTrackDocument.position,
                    Notes = string.IsNullOrEmpty(mlTrackDocument.trackNotes) ? null : mlTrackDocument.trackNotes, 
                    Tempo = string.IsNullOrEmpty(mlTrackDocument.tempo) ? null : mlTrackDocument.tempo                    
                },
                WorkspaceId = mlTrackDocument.wsId,
                Arid = mlTrackDocument.arid,
                Assets = mlTrackDocument.assets,
                Territories = mlTrackDocument.territories
            };

            if(!string.IsNullOrEmpty(mlTrackDocument.isrc))
                track.TrackData.Identifiers["isrc"] = mlTrackDocument.isrc;

            if (!string.IsNullOrEmpty(mlTrackDocument.iswc))
                track.TrackData.Identifiers["iswc"] = mlTrackDocument.iswc;

            if (!string.IsNullOrEmpty(mlTrackDocument.prs))
                track.TrackData.Identifiers["prs"] = mlTrackDocument.prs;

            if (mlTrackDocument.composer?.Count() > 0)
                foreach (var item in mlTrackDocument.composer)
                    interestedParties.Add(new InterestedParty() { Role = nameof(enIPRole.composer), FullName = item });

            if (mlTrackDocument.publisher?.Count() > 0)
                foreach (var item in mlTrackDocument.publisher)
                    interestedParties.Add(new InterestedParty() { Role = nameof(enIPRole.publisher), FullName = item });

            if (mlTrackDocument.lyricist?.Count() > 0)
                foreach (var item in mlTrackDocument.lyricist)
                    interestedParties.Add(new InterestedParty() { Role = nameof(enIPRole.lyricist), FullName = item });

            if (mlTrackDocument.arranger?.Count() > 0)
                foreach (var item in mlTrackDocument.arranger)
                    interestedParties.Add(new InterestedParty() { Role = nameof(enIPRole.arranger), FullName = item });

            if (mlTrackDocument.performer?.Count() > 0)
                foreach (var item in mlTrackDocument.performer)
                    interestedParties.Add(new InterestedParty() { Role = nameof(enIPRole.performer), FullName = item });

            if (mlTrackDocument.translator?.Count() > 0)
                foreach (var item in mlTrackDocument.translator)
                    interestedParties.Add(new InterestedParty() { Role = nameof(enIPRole.translator), FullName = item });

            if (mlTrackDocument.recordLabel?.Count() > 0)
                foreach (var item in mlTrackDocument.recordLabel)
                    interestedParties.Add(new InterestedParty() { Role = nameof(enIPRole.record_label), FullName = item });


            if (!string.IsNullOrEmpty(mlTrackDocument.prodName)) {
                track.TrackData.Product = new Product()
                {
                    Name = mlTrackDocument.prodName,
                    Id = mlTrackDocument.prodId == null || mlTrackDocument.prodId == Guid.Empty ? Guid.NewGuid() : (Guid)mlTrackDocument.prodId,
                    Artist = string.IsNullOrEmpty(mlTrackDocument.prodArtist) ? null : mlTrackDocument.prodArtist,
                    Notes = string.IsNullOrEmpty(mlTrackDocument.prodNotes) ? null : mlTrackDocument.prodNotes,
                    Year = mlTrackDocument.prodYear,
                    ReleaseDate = mlTrackDocument.prodRelease,
                    NumberOfDiscs = string.IsNullOrEmpty(mlTrackDocument.prodNumberOfDiscs) ? null : mlTrackDocument.prodNumberOfDiscs,                  
                    ArtworkUri = string.IsNullOrEmpty(mlTrackDocument.prodArtworkUrl) ? null : mlTrackDocument.prodArtworkUrl,
                    Identifiers = new Dictionary<string, string>()
                };
                if(!string.IsNullOrEmpty(mlTrackDocument.catNo))
                    track.TrackData.Product.Identifiers["catalogue_number"] = mlTrackDocument.catNo;
                if (!string.IsNullOrEmpty(mlTrackDocument.upc))
                    track.TrackData.Product.Identifiers["upc"] = mlTrackDocument.upc;
                if (!string.IsNullOrEmpty(mlTrackDocument.ean))
                    track.TrackData.Product.Identifiers["ean"] = mlTrackDocument.ean;
                if (!string.IsNullOrEmpty(mlTrackDocument.grid))
                    track.TrackData.Product.Identifiers["grid"] = mlTrackDocument.grid;
                if (!string.IsNullOrEmpty(mlTrackDocument.barcode))
                    track.TrackData.Product.Identifiers["barcode"] = mlTrackDocument.barcode;
            }

            if(interestedParties.Count()>0)
                track.TrackData.InterestedParties = interestedParties.ToArray();

            return track;
        }

        public static string GetCtagStatus(ClearanceCTags clearanceCTags, int cTagId)
        {
            string status = null;

            if (clearanceCTags.prsSearchError || clearanceCTags.update == false)
            {
                status = "N/A";
            }
            else if (clearanceCTags.update)
            {
                var ctag = clearanceCTags.cTags.FirstOrDefault(a => a.id == cTagId);
                if (ctag?.result == true)
                {
                    status = "Yes";
                }
                else
                {

                    status = "No";
                }
            }
            return status;
        }

        private static List<string> GetTrackAll(MLTrackDocument mLTrackDocument)
        {
            List<string> trackAll = new List<string>();

            if (mLTrackDocument.performer != null)
                trackAll.AddRange(mLTrackDocument.performer);

            if (mLTrackDocument.composer != null)
                trackAll.AddRange(mLTrackDocument.composer);

            if (mLTrackDocument.lyricist != null)
                trackAll.AddRange(mLTrackDocument.lyricist);

            if (mLTrackDocument.adaptor != null)
                trackAll.AddRange(mLTrackDocument.adaptor);

            if (mLTrackDocument.administrator != null)
                trackAll.AddRange(mLTrackDocument.administrator);

            if (mLTrackDocument.arranger != null)
                trackAll.AddRange(mLTrackDocument.arranger);

            if (mLTrackDocument.publisher != null)
                trackAll.AddRange(mLTrackDocument.publisher);

            if (mLTrackDocument.originalPublisher != null)
                trackAll.AddRange(mLTrackDocument.originalPublisher);

            if (mLTrackDocument.recordLabel != null)
                trackAll.AddRange(mLTrackDocument.recordLabel);

            if (mLTrackDocument.subLyricist != null)
                trackAll.AddRange(mLTrackDocument.subLyricist);

            if (mLTrackDocument.subAdaptor != null)
                trackAll.AddRange(mLTrackDocument.subAdaptor);

            if (mLTrackDocument.subArranger != null)
                trackAll.AddRange(mLTrackDocument.subArranger);

            if (mLTrackDocument.subPublisher != null)
                trackAll.AddRange(mLTrackDocument.subPublisher);

            if (mLTrackDocument.translator != null)
                trackAll.AddRange(mLTrackDocument.translator);

            if (mLTrackDocument.composerLyricist != null)
                trackAll.AddRange(mLTrackDocument.composerLyricist);

            if (mLTrackDocument.prsWorkWriters != null)
                trackAll.AddRange(mLTrackDocument.prsWorkWriters);

            if (mLTrackDocument.prsWorkPublishers != null)
                trackAll.AddRange(mLTrackDocument.prsWorkPublishers);

            if (mLTrackDocument.composerLyricist != null)
                trackAll.AddRange(mLTrackDocument.composerLyricist);

            if (!string.IsNullOrWhiteSpace(mLTrackDocument.prodArtist))
                trackAll.Add(mLTrackDocument.prodArtist);

            if (!string.IsNullOrWhiteSpace(mLTrackDocument.libName))
                trackAll.Add(mLTrackDocument.libName);

            if (!string.IsNullOrWhiteSpace(mLTrackDocument.wsName))
                trackAll.Add(mLTrackDocument.wsName);

            if (mLTrackDocument.allTrackTags.Count() > 0)
                trackAll.AddRange(mLTrackDocument.allTrackTags);

            if (mLTrackDocument.searchContributorsExtended?.Count() > 0)
                trackAll.AddRange(mLTrackDocument.searchContributorsExtended);

            //if (!string.IsNullOrEmpty(mLTrackDocument.prsWorkWriters)) {
            //    trackAll.AddRange(mLTrackDocument.allTrackTags);
            //}


            return trackAll.Distinct().ToList();
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

            if (mLTrackDocument.adminTags?.Count() > 0)
                trackTags.AddRange(mLTrackDocument.adminTags);

            if (!string.IsNullOrEmpty(mLTrackDocument.bpm))
                trackTags.Add(mLTrackDocument.bpm);

            if (!string.IsNullOrEmpty(mLTrackDocument.tempo))
                trackTags.Add(mLTrackDocument.tempo);

            trackTags = trackTags.Distinct().ToList();

            return trackTags;
        }

        public static KeyValuePair<string, string> CleanseOrigin(string origin)
        {
            if(string.IsNullOrEmpty(origin))
                return new KeyValuePair<string, string>(null, null);

            string subOrigin = null;

            if (origin != null && origin.Contains(':'))
                origin = origin.Split(':')[0];

            if (origin.Trim() == enDHMusicOrigin.library.ToString())
            {
                subOrigin = "MCPS";
            }
            else if (origin.Trim() == enDHMusicOrigin.library_non_affiliated.ToString()
               || origin.Trim() == enDHMusicOrigin.library_non_mechanical.ToString())
            {
                origin = enMLMusicOrigin.library.ToString();
                subOrigin = "Non-MCPS";
            }

            return new KeyValuePair<string, string>(origin, subOrigin);
        }

        public static List<string> GetAllAlbumTags(MLTrackDocument mLTrackDocument)
        {
            List<string> trackTags = new List<string>();

            if (mLTrackDocument.prodTags?.Count() > 0)
                trackTags.AddRange(mLTrackDocument.prodTags);

            trackTags = trackTags.Distinct().ToList();

            return trackTags;
        }

        public static List<string> CleanseTags(List<string> tagList)
        {
            //var rx = new Regex(@"\,|/|~+", RegexOptions.Compiled);

            if (tagList?.Count() > 0)
            {
                List<string> cleansedList = new List<string>();
                foreach (var item in tagList)
                {
                    string[] _lst = item.Split(',', '/','~'); 
                    cleansedList.AddRange(_lst.Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)));
                }

                cleansedList = cleansedList.Distinct().ToList();

                return cleansedList;
            }
            else
            {
                return null;
            }
        }

        

        public static List<string> GetNameListByRole(string role, ICollection<Soundmouse.Messaging.Model.InterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0) {
                List<string> mLInterestedParties = interestedParties.Where(a => a.Role == role).Select(a => a.FullName.ReplaceSpecialCodes()).ToList();

                if (mLInterestedParties.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

        public static List<string> GetNameListByRole(string role, ICollection<DHTInterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.role == role).Select(a => a.name).ToList();

                if (mLInterestedParties.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

        public static List<MLInterestedParty> MapIpList(ICollection<Soundmouse.Messaging.Model.InterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0)
            {
                List<MLInterestedParty> mLInterestedParties = interestedParties.Select(a => new MLInterestedParty()
                {
                    role = a.Role,
                    fullName = a.FullName,
                    identifiers = a.IpIdentifiers != null && a.IpIdentifiers.Count > 0 ? a.IpIdentifiers : null,
                    societyAffiliation = a.Society,
                    societyShare = a.Share
                }).ToList();

                if (mLInterestedParties.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

        public static string GetIdentifiersByType(string type, IDictionary<string, string> identifiers)
        {
            return identifiers.FirstOrDefault(a => a.Key == type).Value;
        }        

        public static MLTrackDocument DHTrackToMLTrackDocument(this DHTrack dHTrack, DHAlbum dHAlbum, string artworkUrl,string documentId)
        {
            MLTrackDocument mlTrackDocument = new MLTrackDocument();

            mlTrackDocument.dh_synced = false;

            mlTrackDocument.id = Guid.Parse(documentId);

            if (dHTrack?.id != null )
            {
                mlTrackDocument.dhTrackId = dHTrack.id;
            }                     

            mlTrackDocument.source = "ml";
            mlTrackDocument.prodArtworkUrl = artworkUrl;

            if (dHAlbum != null)
            {
                mlTrackDocument.prodId = dHAlbum.id;
                mlTrackDocument.prodName = dHAlbum.name;
                mlTrackDocument.prodSubtitle = dHAlbum.subtitle;                

                if (!string.IsNullOrEmpty(dHAlbum.releaseDate))
                    mlTrackDocument.prodRelease = DateTime.Parse(dHAlbum.releaseDate);

                mlTrackDocument.prodArtist = dHAlbum.artist;
                mlTrackDocument.prodNotes = dHAlbum.notes;

                if (dHAlbum.identifiers?.Count > 0)
                {
                    mlTrackDocument.catNo = dHAlbum.identifiers.FirstOrDefault(a => a.type == "catalogue_number")?.value;
                    mlTrackDocument.upc = dHAlbum.identifiers.FirstOrDefault(a => a.type == "upc")?.value;
                    mlTrackDocument.ean = dHAlbum.identifiers.FirstOrDefault(a => a.type == "ean")?.value;
                    mlTrackDocument.grid = dHAlbum.identifiers.FirstOrDefault(a => a.type == "grid")?.value;
                    mlTrackDocument.barcode = dHAlbum.identifiers.FirstOrDefault(a => a.type == "barcode")?.value;
                }
            }

            mlTrackDocument.audioFileName = dHTrack.filename;
            mlTrackDocument.libId = dHTrack.libraryId;
            mlTrackDocument.trackTitle = dHTrack.title;
            mlTrackDocument.musicOrigin = dHTrack.musicOrigin;
            mlTrackDocument.alternativeTitle = dHTrack.alternativeTitle;
            mlTrackDocument.duration = dHTrack.duration;
            mlTrackDocument.position = dHTrack.position;
            mlTrackDocument.subIndex = dHTrack.subIndex;
            mlTrackDocument.trackNotes = dHTrack.notes;

            if (dHTrack.descriptiveExtended != null)
            {
                try
                {
                    DescriptiveData obj = dHTrack.descriptiveExtended.SingleOrDefault(a => a.Source == enDescriptiveExtendedSource.ML_UPLOAD.ToString());

                    if (obj != null) {
                        string assetS3Id = obj.Value.GetValueFromObject("AssetS3Id");
                        string size = obj.Value.GetValueFromObject("Size");
                        if (!string.IsNullOrEmpty(assetS3Id))
                        {

                            if (mlTrackDocument.assets == null)
                                mlTrackDocument.assets = new List<Asset>();

                            mlTrackDocument.assets.Add(new Asset()
                            {
                                Key = assetS3Id,
                                BucketName = obj.Value.GetValueFromObject("BucketName"),
                                Type = Path.GetExtension(assetS3Id).TrimStart('.'),
                                Quality = 0,
                                Size = -1
                            });
                        }
                    }                    
                }
                catch (Exception)
                {

                }
            }

            if (dHTrack.identifiers?.Count() > 0)
            { 
                foreach (var item in dHTrack.identifiers)
                {
                    //mlTrackDocument.trackIdentifiers.Add(item.type, item.value);
                    if (item.type == "isrc")
                        mlTrackDocument.isrc = item.value;
                    if (item.type == "iswc")
                        mlTrackDocument.iswc = item.value;
                    if (item.type == "prs")
                        mlTrackDocument.prs = item.value;
                }
            }

            if (dHTrack.interestedParties?.Count() > 0)
            {
                List<string> _ComposerLyricist = GetNameListByRole("composer_lyricist", dHTrack.interestedParties);

                mlTrackDocument.composer = GetNameListByRole("composer", dHTrack.interestedParties);
                mlTrackDocument.lyricist = GetNameListByRole("lyricist", dHTrack.interestedParties);
                mlTrackDocument.adaptor = GetNameListByRole("adaptor", dHTrack.interestedParties);
                mlTrackDocument.administrator = GetNameListByRole("administrator", dHTrack.interestedParties);
                mlTrackDocument.arranger = GetNameListByRole("arranger", dHTrack.interestedParties);
                mlTrackDocument.publisher = GetNameListByRole("publisher", dHTrack.interestedParties);
                mlTrackDocument.originalPublisher = GetNameListByRole("original_publisher", dHTrack.interestedParties);
                mlTrackDocument.performer = GetNameListByRole("performer", dHTrack.interestedParties);
                mlTrackDocument.recordLabel = GetNameListByRole("record_label", dHTrack.interestedParties);
                mlTrackDocument.subLyricist = GetNameListByRole("sub_lyricist", dHTrack.interestedParties);
                mlTrackDocument.subAdaptor = GetNameListByRole("sub_adaptor", dHTrack.interestedParties);
                mlTrackDocument.subArranger = GetNameListByRole("sub_arranger", dHTrack.interestedParties);
                mlTrackDocument.subPublisher = GetNameListByRole("sub_publisher", dHTrack.interestedParties);
                mlTrackDocument.translator = GetNameListByRole("translator", dHTrack.interestedParties);

                if (_ComposerLyricist != null)
                {
                    if (mlTrackDocument.composer == null)
                        mlTrackDocument.composer = new List<string>();

                    if (mlTrackDocument.lyricist == null)
                        mlTrackDocument.lyricist = new List<string>();

                    mlTrackDocument.composer.AddRange(_ComposerLyricist);
                    mlTrackDocument.lyricist.AddRange(_ComposerLyricist);
                }
            }

            mlTrackDocument.moods = dHTrack.moods;
            mlTrackDocument.genres = dHTrack.genres;
            mlTrackDocument.styles = dHTrack.styles;
            mlTrackDocument.instruments = dHTrack.instruments;
            mlTrackDocument.bpm = dHTrack.bpm;
            mlTrackDocument.tempo = dHTrack.tempo;
            mlTrackDocument.keywords = dHTrack.tags;           
            
            return mlTrackDocument;
        }

        public static double? StringToDouble(this string val)
        {
            double douVal = 0;
            if (double.TryParse(val, out douVal))
                return douVal;

            return null;
        }

        public static int? StringToInteger(this string val)
        {
            int intval = 0;
            if (int.TryParse(val, out intval))
                return intval;

            return null;
        }
    }
}
