using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Newtonsoft.Json;
using MusicManager.Application.WebService;
using MusicManager.PrsSearch.Models;
using System.Collections;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MusicManager.Logics.Extensions;
using Soundmouse.Messaging.Model;
using System.Diagnostics;
using System.Text;
using MusicManager.Logics.ServiceLogics;
using Elasticsearch.Util;
using MusicManager.PrsSearch.PrsAuth;

namespace MusicManager.Logics.Logics
{
    public class CTagsLogic : ICtagLogic
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly MLContext _context;
        private readonly IPrsRecording _prsRecording;
        private readonly IPrsWorkDetails _prsWorkDetails;
        private readonly IWork _prsWork;
        private readonly IProduct _prsProduct;
        private readonly ILogger<CTagsLogic> _logger;
        private readonly IElasticLogic _elasticLogic;
        private readonly IAuthentication _prsAuthentication;

        public CTagsLogic(IUnitOfWork unitOfWork, IOptions<AppSettings> appSettings,
            MLContext context,
            IPrsRecording recording, IPrsWorkDetails prsWorkDetails,
            IWork work,
            IProduct product,
            ILogger<CTagsLogic> logger,
            IElasticLogic elasticLogic,
            IAuthentication authentication)
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
            _context = context;
            _prsRecording = recording;
            _prsWorkDetails = prsWorkDetails;
            _prsWork = work;
            _prsProduct = product;
            _logger = logger;
            _elasticLogic = elasticLogic;
            _prsAuthentication = authentication;
        }

        public async Task AddCTag(CTagPayload ctagPayload)
        {

            var tag = await _context.c_tag.OrderByDescending(tag => tag.id).FirstOrDefaultAsync();
            var ctag = new c_tag()
            {
                id = tag.id + 1,
                created_by = Convert.ToInt32(ctagPayload.userId),
                date_created = DateTime.Now,
                description = ctagPayload.description,
                name = ctagPayload.name,
                type = ctagPayload.type,
                colour = ctagPayload.colour,
                display_indicator = ctagPayload.display_indicator,
                indicator = ctagPayload.indicator,
                date_last_edited = DateTime.Now,
                last_edited_by = Convert.ToInt32(ctagPayload.userId),
                status = (int)enCtagStatus.Active,
                group_id = ctagPayload.group_id
            };

            await _unitOfWork.CTags.SaveCtag(ctag);
            log_user_action actionLog = new log_user_action
            {
                data_type = "",
                date_created = DateTime.Now,
                user_id = Convert.ToInt32(ctagPayload.userId),
                org_id = "",
                data_value = "",
                action_id = (int)enActionType.ADD_CTAG,
                ref_id = Guid.Empty, // ws id
                status = 1
            };
            await _unitOfWork.ActionLogger.Log(actionLog);

        }


        public async Task UpdateCTag(CTagPayload ctagPayload)
        {

            var ctag = _unitOfWork.CTags.GetById(ctagPayload.id);

            ctag.description = ctagPayload.description;
            ctag.last_edited_by = Convert.ToInt32(ctagPayload.userId);
            ctag.date_last_edited = DateTime.Now;
            ctag.name = ctagPayload.name;
            ctag.type = ctagPayload.type;
            ctag.colour = ctagPayload.colour;
            ctag.display_indicator = ctagPayload.display_indicator;
            ctag.indicator = ctagPayload.indicator;
            ctag.group_id = ctagPayload.group_id;

            await _unitOfWork.CTags.UpdateCtag(ctag); 
        }

        public async Task AddCTagExtended(CTagExtendedPayload ctagPayload, enCtagRuleStatus enCtagRuleStatus)
        {
            var ctag = new c_tag_extended()
            {
                condition = ctagPayload.rules,
                created_by = Convert.ToInt32(ctagPayload.userId),
                date_created = DateTime.Now,
                c_tag_id = ctagPayload.c_tag_id,
                name = ctagPayload.name,
                color = ctagPayload.color,
                status = ctagPayload.status,
                date_last_edited = DateTime.Now,
                last_edited_by = Convert.ToInt32(ctagPayload.userId),
                track_id = ctagPayload.track_id,
                notes = ctagPayload.notes

            };
            await _unitOfWork.CTagsExtended.SaveCtagExtended(ctag);
        }

        public async Task UpdateCTagExtended(CTagExtendedPayload ctagPayload)
        {
            var cTag = _unitOfWork.CTagsExtended.GetById(ctagPayload.id);

            cTag.id = ctagPayload.id;
            cTag.condition = ctagPayload.rules;
            cTag.last_edited_by = Convert.ToInt32(ctagPayload.userId);
            cTag.date_last_edited = DateTime.Now;
            cTag.c_tag_id = ctagPayload.c_tag_id;
            cTag.name = ctagPayload.name;
            cTag.color = ctagPayload.color;
            cTag.status = ctagPayload.status;
            cTag.notes = ctagPayload.notes;
            await _unitOfWork.CTagsExtended.UpdateCtagExtended(cTag);
           
        }

        public async Task<int> ChangeStatus(CTagArchivePayload ctagPayload)
        {
            int archiveCount = 0;
            foreach (var id in ctagPayload.ids)
            {
                archiveCount = await _unitOfWork.CTags.ChangeStatus(id, ctagPayload.status);
            }
            return archiveCount;
        }

        public async Task<int> ChangeRuleStatus(CTagArchivePayload ctagPayload)
        {
            int check = 0;
            foreach (var id in ctagPayload.ids)
            {
                check = await _unitOfWork.CTagsExtended.ChangeRuleStatus(id, ctagPayload.status);
                if (check == 1)
                {
                    if (ctagPayload.status == (int)enCtagRuleStatus.Active)
                    {
                        c_tag cTag = await _unitOfWork.CTags.GetCtagByRuleId(id);

                        if (cTag != null)
                            await _unitOfWork.CTags.UpdateCtagIndexStatus(new c_tag_index_status()
                            {
                                type = cTag.type,
                                updated_by = !string.IsNullOrEmpty(ctagPayload.userId) ? int.Parse(ctagPayload.userId) : 0,
                                update_idetifier = Guid.NewGuid()
                            });
                    }
                }
            }

            return check;
        }

        public async Task<int> DeleteRule(CTagDeletePayload ctagPayload)
        {
            int deleteCount = 0;
            foreach (var id in ctagPayload.ids)
            {
                deleteCount = await _unitOfWork.CTagsExtended.DeleteRule(id);
            }

            if (deleteCount > 0)
            {
                log_user_action actionLog = new log_user_action
                {
                    data_type = "",
                    date_created = DateTime.Now,
                    user_id = Convert.ToInt32(ctagPayload.userId),
                    org_id = "",
                    data_value = "",
                    action_id = (int)enActionType.DELETE_CTAG_RULE,
                    ref_id = Guid.Empty, // ws id
                    status = 1
                };
                await _unitOfWork.ActionLogger.Log(actionLog);
            }

            return deleteCount;

        }

        public async Task<ClearanceCTags> CheckPRSCTag(PrsPayload payload, MLTrackDocument mLTrackDocument)
        {
            ClearanceCTags clearanceCTags = new ClearanceCTags();
            clearanceCTags.cTags = new List<CTagOrg>();
            clearanceCTags.update = false;

            string _isrc = mLTrackDocument.isrc;
            string _prs = mLTrackDocument.prs;

            List<c_tag> c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;
            c_tag requestedCTags = c_Tags?.FirstOrDefault(a => a.id == payload.ctagId);
            clearanceCTags.reqestedCtagGroup = requestedCTags?.group_id;

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                if (mLTrackDocument != null && !mLTrackDocument.sourceDeleted)
                {                   
                    clearanceCTags.cTags = MapOrgCtags(c_Tags);                  

                    //--- Check dynamic CTags ----------------------------------------------------
                    for (int i = 0; i < clearanceCTags.cTags.Count(); i++)
                    {
                        if (clearanceCTags.cTags[i].type == "dynamic")
                        {
                            CtagRuleCheck ctagRuleCheck = await CheckRules(mLTrackDocument, clearanceCTags.cTags[i].id);

                            clearanceCTags.cTags[i].created = DateTime.Now;
                            clearanceCTags.cTags[i].result = ctagRuleCheck?.result;
                        }
                    }
                    //-----------------------------------------------------------------------------                                         

                    if (requestedCTags?.group_id == 3)
                    {
                        clearanceCTags = PRSSearch(clearanceCTags, mLTrackDocument, c_Tags);

                        //-- If there is PRS match in any search ignore any PRS search errors
                        if (clearanceCTags.prsSearchError == true &&
                            !string.IsNullOrEmpty(clearanceCTags.workTunecode))
                            clearanceCTags.prsSearchError = false;
                    }

                    //Check PPL Label
                    if (payload.ctagId == null || payload.ctagId == (int)enCTagTypes.PPL_LABEL)
                    {
                        int pplLabelTagId = clearanceCTags.cTags.FindIndex(a => a.id == (int)enCTagTypes.PPL_LABEL);
                        if (pplLabelTagId > -1)
                        {
                            clearanceCTags.cTags = await CheckLabelCtags(clearanceCTags.cTags, mLTrackDocument, pplLabelTagId);
                        }
                    }

                    if (requestedCTags?.group_id == 3 &&
                        clearanceCTags.prsSessionNotFound == false &&
                        clearanceCTags.prsSearchError == false)
                    {
                        clearanceCTags.dateTime = DateTime.Now;
                        mLTrackDocument.prsSearchDateTime = clearanceCTags.dateTime;
                        mLTrackDocument.prsSearchError = clearanceCTags.prsSearchError;
                        mLTrackDocument.prsFound = clearanceCTags.update;
                        mLTrackDocument.cTags = clearanceCTags.cTags;                        
                        mLTrackDocument.prsWorkTunecode = clearanceCTags.workTunecode;
                        mLTrackDocument.prsWorkWriters = clearanceCTags.workWriters.SplitByComma();
                        mLTrackDocument.prsWorkPublishers = clearanceCTags.workPublishers.SplitByComma();
                        mLTrackDocument.prsWorkTitle = clearanceCTags.workTitle;
                        mLTrackDocument.cTagMcpsOwner = Elasticsearch.DataMatching.TrackDocumentExtensions.GetCtagStatus(clearanceCTags, (int)enCTagTypes.PRS_MCPS_OWNERSHIP);
                        mLTrackDocument.cTagNorthAmerican = Elasticsearch.DataMatching.TrackDocumentExtensions.GetCtagStatus(clearanceCTags, (int)enCTagTypes.NORTH_AMERICAN_COPYRIGHT);

                        await _elasticLogic.TrackIndex(mLTrackDocument);

                        await _unitOfWork.TrackOrg.UpdateTrackOrgByOriginalTrackId(new track_org
                        {
                            original_track_id = mLTrackDocument.dhTrackId.Value,
                            c_tags = JsonConvert.SerializeObject(clearanceCTags, new JsonSerializerSettings())
                        });
                    }

                    if (requestedCTags?.group_id == 3 &&
                        (clearanceCTags.prsSessionNotFound == true ||
                        clearanceCTags.prsSearchError == true))
                    {
                        string prsSearchError = clearanceCTags.prsSessionNotFound ? "PRS Session error" : clearanceCTags.prsSearchError ? "PRS Search error" : "PRS not found";
                        _logger.LogWarning("PRS not found for Track: {track_id}, Error: {SearchError}  | Module: {Module}", payload.trackId, prsSearchError, "PRS Search");
                    }                   
                }
            }

            //--- If the PRS Api is failed and previous matching is correct return the previous values 
            if (requestedCTags?.group_id == 3 
                && string.IsNullOrEmpty(clearanceCTags.workTunecode)
                && !string.IsNullOrEmpty(mLTrackDocument.prsWorkTunecode))
                return mLTrackDocument.ClearanceCTagsFromMLTrackDocument(clearanceCTags);

            return clearanceCTags;
        }        

        private static string CleanseIdentifier(string original)
        {
            return original?.Replace("-", "")
                            .Replace(".", "")
                            .Replace(" ", "")
                            .Trim();
        }

        private static Dictionary<string, string> GetIdentifiers(Recording recording)
        {
            var identifiers = new Dictionary<string, string>();

            var prs = CleanseIdentifier(recording.Tunecode);

            if (!string.IsNullOrEmpty(prs))
                identifiers["prs"] = prs;

            var isrc = CleanseIdentifier(recording.Isrc);

            if (!string.IsNullOrEmpty(isrc))
                identifiers["isrc"] = isrc;

            identifiers["prs:recording"] = recording.RecordingId.ToString();
            return identifiers;
        }
        private static Track[] TransformWorkToTrack(Work[] works)
        {
            Track[] tracks = works.Select(w => WorkToTrack(w)).ToArray();
            return tracks;
        }
        private static Track WorkToTrack(Work work)
        {
            var track = new Track()
            {
                TrackData = new TrackData()
                {
                    Title = work.arg2.Title,
                    InterestedParties = new List<InterestedParty>(),
                    Identifiers = new Dictionary<string, string>()
                }
            };

            if (work.arg2?.WriterArray?.Count() > 0)
            {
                foreach (var item in work.arg2?.WriterArray)
                {
                    track.TrackData.InterestedParties.Add(new InterestedParty()
                    {
                        FullName = formatWorkIpName(item),
                        Role = enIPRole.composer.ToString()
                    });
                }
            }

            track.TrackData.Identifiers.Add("prs", work.arg2.Tunecode);

            return track;
        }
        private static Track RecordingToTrack(Recording recording)
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
                    AlternativeTitle = recording.AlternateTitle,
                    Duration = recording.Duration,
                    Identifiers = GetIdentifiers(recording),
                    InterestedParties = recording.Artists,                    
                    Title = recording.Title,
                    Product = new Soundmouse.Messaging.Model.Product()
                }
            };

            return track;
        }
        private static Track MlTrackDocToTrack(MLTrackDocument mLTrackDocument)
        {
            return new Track()
            {
                TrackData = new TrackData()
                {
                    Title = mLTrackDocument.trackTitle,
                    Identifiers = mLTrackDocument.trackIdentifiers,
                    InterestedParties = addComposerLyricistToComposer(mLTrackDocument.ips.GetIpsValueToCollection()),
                    Product = new Soundmouse.Messaging.Model.Product()
                    {
                        Identifiers = mLTrackDocument.prodIdentifiers,
                        Name = mLTrackDocument.prodName
                    }
                }
            }; 
        }

        private static Track AppendAlbumToTrack(Track track,Track album)
        {
            track.TrackData.Product = album.TrackData.Product;
            return track;
        }
        private static string formatWorkIpName(string fullName)
        {
            if (!string.IsNullOrEmpty(fullName) && fullName.Contains(',')) {
                string[] nameParts = fullName.Split(',');
                fullName = $"{nameParts[1].Trim()} {nameParts[0].Trim()}";
            }
            return fullName;
        }
        private static ICollection<InterestedParty> addComposerLyricistToComposer(ICollection<InterestedParty> interestedParties)
        {
            var composerLyricist = interestedParties.Where(i => i.Role == enIPRole.composer_lyricist.ToString());

            if (composerLyricist.Count() > 0) {
                var newComposers = composerLyricist.Where(x => !interestedParties.Any(y => x.FullName == y.FullName && y.Role == enIPRole.composer.ToString()));

                foreach (var item in newComposers.ToList())
                {
                    interestedParties.Add(new InterestedParty()
                    {
                        FullName = item.FullName,
                        Role = enIPRole.composer.ToString()
                    });
                }
            }            
            return interestedParties;
        }

        public ClearanceCTags PRSSearch(ClearanceCTags clearanceCTags,
            MLTrackDocument mLTrackDocument,
            List<c_tag> c_Tags)
        {          

            string _prs = !string.IsNullOrEmpty(mLTrackDocument.prs) ? mLTrackDocument.prs.Replace("\t", "").Trim() : "";
            bool workFound = false;            

            Regex regexTunecode = new Regex(@"^[0-9]+[a-z]{1,2}$", RegexOptions.IgnoreCase);
            Regex regexISRC = new Regex(@"^[A-Z]{2}-?\w{3}-?\d{2}-?\d{5}$", RegexOptions.IgnoreCase);

            //--- Check source PRS format
            if (!string.IsNullOrEmpty(_prs) 
                && (_prs.Length > 8 || !regexTunecode.IsMatch(_prs))) {               
                _logger.LogWarning("PRS Search > Datahub tunecode is incorrect {Tunecode} | {TrackId}", _prs, mLTrackDocument.id);
                _prs = string.Empty;
            }

            //-- Check source ISRC format
            if (mLTrackDocument.isrc!=null && !regexISRC.IsMatch(mLTrackDocument.isrc))
            {
                _logger.LogWarning("PRS Search > Datahub ISRC is incorrect {ISRC} | {TrackId}", mLTrackDocument.isrc, mLTrackDocument.id);
                mLTrackDocument.isrc = string.Empty;
            }

            if (clearanceCTags == null)
            {
                clearanceCTags = new ClearanceCTags();
                clearanceCTags.cTags = new List<CTagOrg>();
                clearanceCTags.update = false;

                if (mLTrackDocument.cTags != null)
                {
                    clearanceCTags.cTags = mLTrackDocument.cTags;                   
                }
                clearanceCTags.cTags = MapOrgCtags(c_Tags);
            }

            //--- Check PRS Session id 
            var prsSessionToken = _prsAuthentication.GetSessionToken();
            if (string.IsNullOrEmpty(prsSessionToken)) {
                clearanceCTags.prsSessionNotFound = true;
                clearanceCTags.prsSearchError = true;
            }                

            //--- If the PRS session id is not returned don't search PRS info 
            if (clearanceCTags.prsSessionNotFound == false) {
                Track _trackDoc = MlTrackDocToTrack(mLTrackDocument);   

                #region --- Search by Tunecode (Work Search - No Match)
                if (!string.IsNullOrEmpty(_prs))
                {
                    clearanceCTags = CheckPrsCTAgsByTunecode(clearanceCTags, _prs, enPrsSearchType.TUNECODE, null, mLTrackDocument.dhTrackId, out workFound);
                    if (workFound)
                        clearanceCTags.update = true;
                }
                #endregion  

                #region --- Search by ISRC (Recording Search)
                if (!workFound)
                {
                    if (!string.IsNullOrEmpty(mLTrackDocument.isrc))
                    {
                        Track matchingTrack = null;

                        try
                        {
                            clearanceCTags.prsQueryCount++;
                            Recording[] recordings = _prsRecording.GetRecordingByIsrc(mLTrackDocument.isrc, mLTrackDocument.dhTrackId);

                            //------- Match without product details
                            matchingTrack = _prsRecording.GetRecordingMatches(_trackDoc, recordings);

                            //------- If not matching recording then get product details and match
                            //if (recordings.Count() > 0 && matchingTrack == null)
                            //{
                            //    clearanceCTags.prsQueryCount = clearanceCTags.prsQueryCount + recordings.Count();
                            //    matchingTrack = SearchProductDBandMatch(recordings,null, _trackDoc);                                
                            //}

                            if (matchingTrack != null)
                            {
                                matchingTrack?.TrackData?.Identifiers?.TryGetValue("prs", out _prs);

                                if (matchingTrack != null && !string.IsNullOrEmpty(_prs))
                                {
                                    clearanceCTags = CheckPrsCTAgsByTunecode(clearanceCTags, _prs, enPrsSearchType.ISRC, matchingTrack, mLTrackDocument.dhTrackId, out workFound);
                                    if (workFound)
                                        clearanceCTags.update = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            clearanceCTags.prsSearchError = true;
                            _logger.LogWarning(ex, "PRS Search > PRS Recording Search > " + mLTrackDocument.id);
                        }
                    }
                }
                #endregion

                #region --- Search by Title and Artist (Recording Search)
                if (!workFound)
                {
                    try
                    {
                        Recording[] recordingsTitleArtist = null;
                        var performers = GetNameListByRole(enIPRole.performer.ToString(), _trackDoc.TrackData.InterestedParties);

                        if (performers?.Count > 0 &&
                            !string.IsNullOrEmpty(_trackDoc.TrackData.Title))
                        {
                            if (performers.Where(a => a.Trim().Length > 1).Count() > 0)
                            {
                                clearanceCTags.prsQueryCount++;
                                recordingsTitleArtist = _prsRecording.GetRecordingsByTitleArtist(_trackDoc.TrackData.Title, string.Join(",", performers), mLTrackDocument.dhTrackId);
                            }
                        }

                        if (recordingsTitleArtist?.Count() > 0)
                        {
                            Track matchingTrack = null;

                            //------- Match without product details
                            matchingTrack = _prsRecording.GetRecordingMatches(_trackDoc, recordingsTitleArtist);

                            //------- If not matching recording then get product details and match
                            //if (matchingTrack == null)
                            //{
                            //    clearanceCTags.prsQueryCount = clearanceCTags.prsQueryCount + recordingsTitleArtist.Count();
                            //    matchingTrack = SearchProductDBandMatch(recordingsTitleArtist,null, _trackDoc);                               
                            //}

                            if (matchingTrack?.TrackData?.Identifiers != null)
                                matchingTrack.TrackData.Identifiers?.TryGetValue("prs", out _prs);

                            if (matchingTrack != null && !string.IsNullOrEmpty(_prs))
                            {
                                clearanceCTags = CheckPrsCTAgsByTunecode(clearanceCTags, _prs, enPrsSearchType.TITLE_PERFORMER, matchingTrack, mLTrackDocument.dhTrackId, out workFound);
                                if (workFound)
                                    clearanceCTags.update = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        clearanceCTags.prsSearchError = true;
                        _logger.LogWarning(ex, "PRS Search > PRS Title artist Search > " + mLTrackDocument.id);
                    }
                }
                #endregion

                #region --- Search by Title and Composer (Work Search)
                //if (!workFound)
                //{
                //    try
                //    {
                //        Work[] works = null;
                        
                //        List<string> composers = GetNameListByRole(enIPRole.composer.ToString(), _trackDoc.TrackData.InterestedParties);

                //        if (composers?.Count > 0 &&
                //            !string.IsNullOrEmpty(_trackDoc.TrackData.Title))
                //        {
                //            if (composers.Where(a => a.Trim().Length > 1).Count() > 0)
                //            {
                //                clearanceCTags.prsQueryCount++;
                //                works = _prsWork.GetWorksByTitleWriters(_trackDoc.TrackData.Title, string.Join(",", composers));
                //            }
                //        }

                //        if (works?.Length > 0)
                //        {
                //            Track track = _prsRecording.GetTrackMatches(_trackDoc, TransformWorkToTrack(works));

                //            //track = SearchProductDBandMatch(null, TransformWorkToTrack(works), _trackDoc);

                //            if (track?.TrackData?.Identifiers != null)
                //                track.TrackData.Identifiers?.TryGetValue("prs", out _prs);

                //            if (track != null && !string.IsNullOrEmpty(_prs))
                //            {
                //                clearanceCTags = CheckPrsCTAgsByTunecode(clearanceCTags, _prs, enPrsSearchType.TITLE_PERFORMER, track, mLTrackDocument.dhTrackId.Value, out workFound);
                //                if (workFound)
                //                    clearanceCTags.update = true;
                //            }
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        clearanceCTags.prsSearchError = true;
                //        _logger.LogWarning(ex, "PRS Search > PRS Title artist Search > " + mLTrackDocument.id);
                //    }
                //}
                #endregion                                           
              
            }

            return clearanceCTags;
        }

        private Track SearchProductDBandMatch(Recording[] recordings, Track[] prsTracks, Track sourceTrack)
        {
            List<Track> tracks = new List<Track>();

            if (recordings != null)
            {
                foreach (var recording in recordings)
                {
                    var products = _prsProduct.GetProductByRecordingId(recording.RecordingId);

                    foreach (var product in products)
                    {
                        tracks.Add(AppendAlbumToTrack(RecordingToTrack(recording), product));
                    }
                }
            }
            else {
                foreach (var prsTrack in prsTracks)
                {
                    var products = _prsProduct.GetProductByTuneCode(prsTrack.TrackData?.Identifiers.FirstOrDefault(a => a.Key == "prs").Value);

                    foreach (var product in products)
                    {
                        tracks.Add(AppendAlbumToTrack(prsTrack, product));
                    }
                }
            }            
            return _prsRecording.GetTrackMatches(sourceTrack, tracks.ToArray());
        }

        private static List<string> GetNameListByRole(string role, ICollection<Soundmouse.Messaging.Model.InterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.Role == role).Select(a => a.FullName).ToList();

                if (mLInterestedParties.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

        private static List<string> GetMLNameListByRole(string role, List<MLInterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.role == role).Select(a => a.fullName).ToList();

                if (mLInterestedParties.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }


        public ClearanceCTags GetPRSWorkDetailsByTunecode(string tunecode)
        {
            ClearanceCTags clearanceCTags = new ClearanceCTags();

            Work work = _prsWorkDetails.GetWorkDetailsByTuneCode(tunecode);
           
            if (work == null)
                return clearanceCTags;

            clearanceCTags.dateTime = DateTime.Now;
            clearanceCTags.workWriters = string.Join(',', work.Writers?.Select(a => a.FullName));
            clearanceCTags.workPublishers = string.Join(',', work.Publishers?.Select(a => a.FullName));
            clearanceCTags.workTitle = work.Title;
            clearanceCTags.workTunecode = work.Tunecode;

            return clearanceCTags;
        }

        private ClearanceCTags CheckPrsCTAgsByTunecode(ClearanceCTags clearanceCTags, string tunecode, enPrsSearchType prsSearchType, Track track, Guid? dhTrackId, out bool found)
        {
            bool _4 = false;
            bool _6 = false;
            bool _2 = false;
            bool _3 = false;
            bool _5 = false;
            found = false;

            try
            {
                if (tunecode.Length > 8)
                    return clearanceCTags;

                clearanceCTags.prsQueryCount++;
                Work work = _prsWorkDetails.GetWorkDetailsByTuneCode(tunecode, dhTrackId);
                if (work == null)
                    return clearanceCTags;

                found = true;

                decimal? _MCPSClaimsPercentage = work.MechanicalShareSummary?.SingleOrDefault(a => a.Category == "MCPS Claims")?.MechanicalSharePercentage;
                decimal? _PublicDomainPercentage = work.MechanicalShareSummary?.SingleOrDefault(a => a.Category == "Public Domain")?.MechanicalSharePercentage;

                if (_MCPSClaimsPercentage != null && _MCPSClaimsPercentage > 0)
                {
                    _5 = true;

                    if (_MCPSClaimsPercentage == 100)
                        _4 = true;
                }
                
                if (_PublicDomainPercentage != null && _PublicDomainPercentage == 100)
                    _3 = true;

                string[] _societyList = { "ascap", "bmi", "sesac", "socan", "non-society" };

                bool? nac = work.Publishers?.Where(x => x.Role.ToLower() == "original publisher")?.Select(x => x.PerformingRightAffiliationField.ToLower())?.Any(_societyList.Contains);
                if(nac==true)
                    _6 = true;

                if (!string.IsNullOrEmpty(work.PriorApprovalCode))
                    _2 = true;

                for (int i = 0; i < clearanceCTags.cTags.Count(); i++)
                {
                    switch (clearanceCTags.cTags[i].id)
                    {
                        case (int)enCTagTypes.PRS_MCPS_OWNERSHIP:
                            clearanceCTags.cTags[i].created = DateTime.Now;
                            clearanceCTags.cTags[i].result = _4;
                            break;

                        case (int)enCTagTypes.NORTH_AMERICAN_COPYRIGHT:
                            clearanceCTags.cTags[i].created = DateTime.Now;
                            clearanceCTags.cTags[i].result = _6;
                            break;

                        case (int)enCTagTypes.PRS_PRIOR_APPROVAL_CODE:
                            clearanceCTags.cTags[i].created = DateTime.Now;
                            clearanceCTags.cTags[i].result = _2;
                            break;

                        case (int)enCTagTypes.MCPS_MUSIC:
                            clearanceCTags.cTags[i].created = DateTime.Now;
                            clearanceCTags.cTags[i].result = _5;
                            break;

                        case (int)enCTagTypes.PRS_PUBLIC_DOMAIN:
                            clearanceCTags.cTags[i].created = DateTime.Now;
                            clearanceCTags.cTags[i].result = _3;
                            break;

                        default:
                            break;
                    }
                }

                clearanceCTags.dateTime = DateTime.Now;
                clearanceCTags.workWriters = string.Join(',', work.Writers?.Select(a => a.FullName));
                clearanceCTags.workPublishers = string.Join(',', work.Publishers?.Select(a => a.FullName));
                clearanceCTags.workTitle = work.Title;
                clearanceCTags.workTunecode = work.Tunecode;

                //Object Mapping for MLPRS LOG
                MLPRS mlprs = new MLPRS
                {
                    searchType = prsSearchType,
                    work = new PRSWork
                    {
                        Iswc = work.Iswc,
                        LibraryCatalogueNumbers = work.LibraryCatalogueNumbers,
                        Publishers = work.Publishers,
                        Title = work.Title,
                        Tunecode = work.Tunecode,
                        Type = work.Type,
                        Writers = work.Writers
                    },
                    recording = track,
                    dateTime = clearanceCTags.dateTime
                };

            }
            catch (Exception ex)
            {                
                clearanceCTags.prsSearchError = true;
                _logger.LogError(ex, "CheckPrsCTAgsByTunecode {TuneCode} | Module:{Module}", tunecode,"PRS Search");
            }
            return clearanceCTags;
        }        

        private List<CTagOrg> MapOrgCtags(List<c_tag> cTags)
        {
            List<CTagOrg> newCTagOrgs = new List<CTagOrg>();
            foreach (var item in cTags)
            {
                newCTagOrgs.Add(new CTagOrg()
                {
                    created = item.date_created,
                    id = item.id,
                    groupId = item.group_id,
                    name = item.name,
                    type = item.type,
                    result = null
                });
            }
            return newCTagOrgs;
        }

        private async Task<List<CTagOrg>> CheckLabelCtags(List<CTagOrg> cTagOrgs, MLTrackDocument mLTrackDocument, int tagIndex)
        {
            try
            {
                if (mLTrackDocument.recordLabel?.Count() > 0 || mLTrackDocument.performer?.Count() > 0)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append("select 1 from ppl_label_search pls where false ");

                    if (mLTrackDocument.recordLabel?.Count() > 0)
                        foreach (var label in mLTrackDocument.recordLabel)
                        {
                            stringBuilder.AppendFormat(" or pls.label ~* '( |^){0}([^A-z]|$)' or pls.member ~* '( |^){0}([^A-z]|$)' ", label.PPLLabelSearchReplace());
                        }

                    if (mLTrackDocument.performer?.Count() > 0)
                        foreach (var performer in mLTrackDocument.performer)
                        {
                            stringBuilder.AppendFormat(" or pls.label ~* '( |^){0}([^A-z]|$)' or pls.member ~* '( |^){0}([^A-z]|$)' ", performer.PPLLabelSearchReplace());
                        }

                    stringBuilder.AppendFormat(" limit 1;");

                    using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                    {
                        var count = await c.ExecuteScalarAsync<int>(stringBuilder.ToString());
                        if (count > 0)
                        {
                            cTagOrgs[tagIndex].created = DateTime.Now;
                            cTagOrgs[tagIndex].result = true;
                        }
                        else
                        {
                            cTagOrgs[tagIndex].created = DateTime.Now;
                            cTagOrgs[tagIndex].result = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckLabelCtags > Track Id : " + mLTrackDocument.dhTrackId);
            }

            //-- Check dynamic Ctag
            if (cTagOrgs[tagIndex].result == false)
            {
                CtagRuleCheck ctagRuleCheck = await CheckRules(mLTrackDocument, _appSettings.Value.PPLCtagId);
                cTagOrgs[tagIndex].result = ctagRuleCheck?.result;
            }

            return cTagOrgs;
        }

        public async Task<CtagRuleCheck> CheckRules(MLTrackDocument mLTrackDocument, int cTagId)
        {

            bool? isRestricted = false;
            CtagRuleCheck ctagRuleCheck = new CtagRuleCheck();
            RuleDetails ruleDetails = new RuleDetails();

            List<string> dynamicListValues = new List<string>();
            List<TagCondition> ruleList = new List<TagCondition>();
            List<c_tag_extended> cTagExtendeds = await _unitOfWork.CTagsExtended.GetActiveRules(cTagId);

            int RuleId = 0;
            
            //--- Check whether rules exist for track id and bring it top
            var cTagExtendedForTrack = cTagExtendeds.FirstOrDefault(a=>a.track_id == mLTrackDocument.id.ToString());
            if (cTagExtendedForTrack != null) {
               cTagExtendeds.Insert(0,cTagExtendedForTrack);                
            }

            try
            {
                foreach (c_tag_extended item in cTagExtendeds)
                {
                    RuleId = item.id;

                    //if (RuleId == 989)
                    //{
                    //    string a = "";
                    //}

                    if (!string.IsNullOrWhiteSpace(item.condition))
                        ruleList = JsonConvert.DeserializeObject<List<TagCondition>>(item.condition);

                    ruleList = CheckConditions(ruleList, cTagId);

                    if (ruleList.Count() > 0)
                    {
                        int count = 0;

                        foreach (var rule in ruleList)
                        {

                            if (!(rule.and == null && rule.defaultItem == null && (rule.or == null || rule.or.Count() == 0)))
                            {
                                dynamic dynamicValue = null;
                                object convertedValue = new object();
                                EnTagCondition condition = 0;
                                string property = string.Empty;
                                string conditionValue = string.Empty;

                                if (rule?.defaultItem != null)
                                {
                                    property = rule?.defaultItem.property;
                                    conditionValue = rule?.defaultItem.value.Trim().ToLower();
                                    dynamicValue = mLTrackDocument.GetType().GetProperty(property);

                                    convertedValue = dynamicValue?.GetValue(mLTrackDocument, null);
                                    condition = (EnTagCondition)rule?.defaultItem.condition;
                                }
                                else if (rule?.and != null)
                                {
                                    property = rule?.and.property;
                                    conditionValue = rule?.and.value.Trim().ToLower();

                                    dynamicValue = mLTrackDocument.GetType().GetProperty(property);
                                    convertedValue = dynamicValue?.GetValue(mLTrackDocument, null);
                                    condition = (EnTagCondition)rule?.and.condition;

                                }
                                else
                                {
                                    if (rule?.or.Count > 0)
                                    {
                                        property = rule?.or[0].property;
                                        conditionValue = rule?.or[0].value.Trim().ToLower();

                                        dynamicValue = mLTrackDocument?.GetType().GetProperty(property);
                                        convertedValue = dynamicValue?.GetValue(mLTrackDocument, null);
                                        condition = (EnTagCondition)rule?.or[0].condition;
                                    }
                                }


                                // Check if its a value or a list
                                if (convertedValue is IList || convertedValue is IDictionary)
                                {
                                    dynamicListValues = (List<string>)(IList)convertedValue;

                                    switch (condition)
                                    {
                                        case EnTagCondition.Exact:
                                            if (dynamicListValues != null && dynamicListValues.Exists(x => x.ToLower().Trim() == conditionValue))
                                                isRestricted = true;
                                            else if (!rule.inlineCond)
                                                isRestricted = false;
                                            break;
                                        case EnTagCondition.Contains:
                                            if (dynamicListValues != null && dynamicListValues.Exists(x => x.ToLower().Trim().Contains(conditionValue)))
                                                isRestricted = true;
                                            else if (!rule.inlineCond)
                                                isRestricted = false;
                                            break;
                                        case EnTagCondition.StartWith:
                                            if (dynamicListValues != null && dynamicListValues.Exists(x => x.ToLower().Trim().StartsWith(conditionValue)))
                                                isRestricted = true;
                                            else if (!rule.inlineCond)
                                                isRestricted = false;
                                            break;
                                        case EnTagCondition.EndWith:
                                            if (dynamicListValues != null && dynamicListValues.Exists(x => x.ToLower().Trim().EndsWith(conditionValue)))
                                                isRestricted = true;
                                            else if (!rule.inlineCond)
                                                isRestricted = false;
                                            break;
                                        case EnTagCondition.NotContains:
                                            if (dynamicListValues != null && !dynamicListValues.Exists(x => x.ToLower().Trim().Contains(conditionValue)))
                                                isRestricted = true;
                                            else if (!rule.inlineCond)
                                                isRestricted = false;
                                            break;
                                        case EnTagCondition.ExactWord:
                                            if (dynamicListValues != null && dynamicListValues.Exists(x => Regex.IsMatch(x.ToLower().Trim(), $@"\b{conditionValue}\b")))
                                                isRestricted = true;
                                            else if (!rule.inlineCond)
                                                isRestricted = false;
                                            break;
                                        case EnTagCondition.Boolean:
                                            if (dynamicListValues != null && dynamicListValues.Exists(x => x.ToLower() == "true"))
                                                isRestricted = true;
                                            else if (!rule.inlineCond)
                                                isRestricted = false;
                                            break;
                                        default:
                                            break;
                                    }
                                    count++;
                                    if (count < ruleList.Count)
                                    {
                                        if (ruleList[count].inlineCond == false && (ruleList[count].and == null && isRestricted == true) || (ruleList[count].or == null && isRestricted == false))
                                            break;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    if (convertedValue == null)
                                        convertedValue = "";

                                    if (convertedValue != null)
                                    {
                                        switch (condition)
                                        {
                                            case EnTagCondition.Exact:
                                                if (convertedValue.ToString().Trim().ToLower() == conditionValue)
                                                    isRestricted = true;
                                                else if (!rule.inlineCond)
                                                    isRestricted = false;
                                                break;
                                            case EnTagCondition.Contains:
                                                if (convertedValue.ToString().Trim().ToLower().Contains(conditionValue))
                                                    isRestricted = true;
                                                else if (!rule.inlineCond)
                                                    isRestricted = false;
                                                break;
                                            case EnTagCondition.StartWith:
                                                if (convertedValue.ToString().Trim().ToLower().StartsWith(conditionValue))
                                                    isRestricted = true;
                                                else if (!rule.inlineCond)
                                                    isRestricted = false;
                                                break;
                                            case EnTagCondition.EndWith:
                                                if (convertedValue.ToString().Trim().ToLower().EndsWith(conditionValue))
                                                    isRestricted = true;
                                                else if (!rule.inlineCond)
                                                    isRestricted = false;
                                                break;
                                            case EnTagCondition.NotContains:
                                                if (!convertedValue.ToString().Trim().ToLower().Contains(conditionValue))
                                                    isRestricted = true;
                                                else if (!rule.inlineCond)
                                                    isRestricted = false;
                                                break;
                                            case EnTagCondition.ExactWord:
                                                if (Regex.IsMatch(convertedValue.ToString().Trim().ToLower(), $@"\b{conditionValue}\b"))
                                                    isRestricted = true;
                                                else if (!rule.inlineCond)
                                                    isRestricted = false;
                                                break;
                                            case EnTagCondition.Boolean:
                                                if (convertedValue.ToString().Trim().ToLower() == "true")
                                                    isRestricted = true;
                                                else if (!rule.inlineCond)
                                                    isRestricted = false;
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    count++;
                                    if (count < ruleList.Count)
                                    {
                                        if (ruleList[count].inlineCond == false && (ruleList[count].and == null && isRestricted == true) || (ruleList[count].or == null && isRestricted == false))
                                            break;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if ((bool)isRestricted == true)
                    {
                        ctagRuleCheck.conditions = ruleList;
                        ruleDetails.ruleId = item.id;
                        ruleDetails.ruleName = item.name;
                        ruleDetails.notes = item.notes;
                        ctagRuleCheck.ruleDetails = ruleDetails;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CheckRules > ({cTagId} {RuleId})");
                isRestricted = null;
            }

            ctagRuleCheck.result = isRestricted;

            return ctagRuleCheck;
        }

        private List<TagCondition> CheckConditions(List<TagCondition> tagConditions,int cTagId)
        {
            List<TagCondition> output = new List<TagCondition>();

            if (tagConditions?.Count() > 0)
            {
                foreach (TagCondition item in tagConditions)
                {
                    if ((item.and != null && item.and.condition != null && !string.IsNullOrEmpty(item.and.property) && !string.IsNullOrEmpty(item.and.value)) ||
                        (item.defaultItem != null && item.defaultItem.condition != null && !string.IsNullOrEmpty(item.defaultItem.property) && !string.IsNullOrEmpty(item.defaultItem.value)) ||
                       (item.or != null && item.or.Count() > 0 && item.or[0].condition != null && !string.IsNullOrEmpty(item.or[0].property) && !string.IsNullOrEmpty(item.or[0].value)))
                    {
                        string _value = "";

                        CTagRulesPayload cTagRulesPayload1 = new CTagRulesPayload();
                        bool isAnd = false;
                        bool isDefault = false;

                        if (!string.IsNullOrEmpty(item.defaultItem?.value))
                        {
                            isDefault = true;
                            cTagRulesPayload1 = item.defaultItem;
                            _value = item.defaultItem?.value;
                        }
                        else if (!string.IsNullOrEmpty(item.and?.value))
                        {
                            isAnd = true;
                            _value = item.and?.value;
                            cTagRulesPayload1 = item.and;
                        }
                        else if (!string.IsNullOrEmpty(item.or?[0].value))
                        {
                            _value = item.or?[0].value;
                            cTagRulesPayload1 = item.or?[0];
                        }

                        //--- Don't split value using comma for explicit ctag because this is generated automatically  
                        if (cTagId != _appSettings.Value.ExplicitCtagId && _value.Contains(','))
                        {
                            string[] valueList = _value.Split(',', StringSplitOptions.RemoveEmptyEntries);

                            for (int i = 0; i < valueList.Count(); i++)
                            {
                                if (i == 0)
                                {
                                    if (isAnd)
                                    {
                                        output.Add(new TagCondition()
                                        {
                                            and = new CTagRulesPayload()
                                            {
                                                condition = cTagRulesPayload1.condition,
                                                value = valueList[i].Trim(),
                                                property = cTagRulesPayload1.property,
                                                innerProperty = cTagRulesPayload1.innerProperty,
                                            }
                                        });
                                    }
                                    else if (isDefault)
                                    {
                                        output.Add(new TagCondition()
                                        {
                                            defaultItem = new CTagRulesPayload()
                                            {
                                                condition = cTagRulesPayload1.condition,
                                                value = valueList[i].Trim(),
                                                property = cTagRulesPayload1.property,
                                                innerProperty = cTagRulesPayload1.innerProperty
                                            }
                                        });
                                    }
                                }
                                else
                                {
                                    output.Add(new TagCondition()
                                    {
                                        inlineCond = true,
                                        or = new List<CTagRulesPayload>() {
                                        new CTagRulesPayload(){
                                            condition = cTagRulesPayload1.condition,
                                            value = valueList[i].Trim(),
                                            property = cTagRulesPayload1.property,
                                            innerProperty = cTagRulesPayload1.innerProperty
                                        }
                                    }
                                    });
                                }
                            }
                        }
                        else
                        {
                            output.Add(item);
                        }
                    }
                }
            }
            return output;
        }


        public IEnumerable<c_tag_extended> GetCtagExtendedByCTagId(int cTagId)
        {
            return _unitOfWork.CTagsExtended.Find(a => a.c_tag_id == cTagId);
        }

        public async Task<c_tag_extended> GetCtagByTrackId(string trackId)
        {
            return await _unitOfWork.CTagsExtended.GetRuleByTrackId(trackId);
        }

        public async Task<List<IndicateCTag>> CheckDynamicDisplayCtag(Guid[] trackIds, int cTagId)
        {
            List<IndicateCTag> result = new List<IndicateCTag>();

            IEnumerable<c_tag> cTags = await _unitOfWork.CTags.GetDynamicDisplayCtags();

            try
            {
                if (cTags.Count() > 0)
                {
                    IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.GetTrackElasticTrackListByIds(trackIds);
                    if (mLTrackDocuments.Count() > 0)
                    {
                        foreach (MLTrackDocument item in mLTrackDocuments)
                        {
                            IndicateCTag indicateCTag = new IndicateCTag();
                            indicateCTag.trackId = item.id;
                            indicateCTag.cTagList = new List<ICTag>();

                            foreach (var cTag in cTags)
                            {
                                CtagRuleCheck ctagRuleCheck = await CheckRules(item, cTag.id);

                                if (ctagRuleCheck?.result == true)
                                {
                                    indicateCTag.cTagList.Add(new ICTag()
                                    {
                                        ctagId = cTag.id,
                                        name = cTag.name,
                                        colour = cTag.colour,
                                        indicator = cTag.indicator,
                                        result = true
                                    });
                                }
                            }

                            if (indicateCTag.cTagList.Count() > 0)
                                result.Add(indicateCTag);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        public async Task<List<IndicateCTagWithRule>> GetCtagRuleListByTrackIdAndCtagId(Guid trackId, List<int> cTagIds)
        {
            try
            {
                List<IndicateCTagWithRule> indicateCTagWithRuleList = new List<IndicateCTagWithRule>();
                MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocById(trackId);
                if (mLTrackDocument != null)
                {
                    foreach (var cTagId in cTagIds)
                    {
                        c_tag c_Tag = await _unitOfWork.CTags.GetCtagById(cTagId);
                        CtagRuleCheck ctagRuleCheck = await CheckRules(mLTrackDocument, cTagId);
                        if (ctagRuleCheck?.result == true)
                        {
                            IndicateCTagWithRule indicateCTagWithRule = new IndicateCTagWithRule
                            {
                                cTag = new ICTag
                                {
                                    ctagId = c_Tag.id,
                                    name = c_Tag.name,
                                    colour = c_Tag.colour,
                                    indicator = c_Tag.indicator,
                                },
                                conditions = ctagRuleCheck.conditions,
                                ruleDetails = ctagRuleCheck.ruleDetails
                            };
                            indicateCTagWithRuleList.Add(indicateCTagWithRule);

                        }
                    }

                }
                return indicateCTagWithRuleList;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<ExplicitCTag>> CheckExplicitCtag(Guid[] trackIds, int cTagId)
        {
            List<ExplicitCTag> result = new List<ExplicitCTag>();

            c_tag c_Tag = await _unitOfWork.CTags.GetCtagById(cTagId);

            if (c_Tag != null)
            {
                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.GetTrackElasticTrackListByIds(trackIds);
                foreach (MLTrackDocument item in mLTrackDocuments)
                {
                    CtagRuleCheck ctagRuleCheck = await CheckRules(item, cTagId);

                    result.Add(new ExplicitCTag()
                    {
                        colour = c_Tag.colour,
                        trackId = item.id,
                        indicator = c_Tag.indicator,
                        result = ctagRuleCheck?.result
                    });
                }
            }
            return result;
        }

        public async Task<PRSUpdateReturn> UpdatePRSforTrack(Guid? mlTrackId,
            MLTrackDocument mLTrackDocument,
            List<c_tag> c_Tags,
            bool charted,
            bool index = true)
        {
            PRSUpdateReturn pRSUpdateReturn = new PRSUpdateReturn();

            try
            {
                if (c_Tags == null)
                    c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;

                if (mLTrackDocument == null && mlTrackId != null)
                    mLTrackDocument = await _elasticLogic.GetElasticTrackDocById((Guid)mlTrackId);

                if (mLTrackDocument != null)
                {
                    ClearanceCTags clearanceCTags = PRSSearch(null, mLTrackDocument, c_Tags);

                    pRSUpdateReturn.prsSessionNotFound = clearanceCTags.prsSessionNotFound;
                    pRSUpdateReturn.prsSearchError = clearanceCTags.prsSearchError;

                    if (charted == true)
                    {
                        clearanceCTags.charted = charted;
                        mLTrackDocument.charted = charted;
                    }

                    if (!clearanceCTags.prsSessionNotFound)
                    {
                        pRSUpdateReturn.prsFound = string.IsNullOrEmpty(clearanceCTags.workTunecode) ? false : true;
                        clearanceCTags.dateTime = DateTime.Now;
                        mLTrackDocument.prsSearchDateTime = clearanceCTags.dateTime;
                        mLTrackDocument.prsFound = pRSUpdateReturn.prsFound;
                        mLTrackDocument.prsSearchError = clearanceCTags.prsSearchError;
                        mLTrackDocument.cTags = clearanceCTags.cTags;                       
                        mLTrackDocument.prsWorkTunecode = clearanceCTags.workTunecode;
                        mLTrackDocument.prsWorkWriters = clearanceCTags.workWriters.SplitByComma();
                        mLTrackDocument.prsWorkPublishers = clearanceCTags.workPublishers.SplitByComma();
                        mLTrackDocument.prsWorkTitle = clearanceCTags.workTitle;
                        mLTrackDocument.dateLastEdited = DateTime.Now;

                        await _unitOfWork.TrackOrg.UpdateTrackOrgByOriginalTrackId(new track_org
                        {
                            original_track_id = mLTrackDocument.dhTrackId.Value,
                            c_tags = JsonConvert.SerializeObject(clearanceCTags, new JsonSerializerSettings()),
                            org_id = mLTrackDocument.org_id
                        });
                        await _elasticLogic.TrackIndex(mLTrackDocument);
                    }
                    pRSUpdateReturn.mLTrackDocument = mLTrackDocument;
                }
                else
                {
                    _logger.LogWarning($"UpdatePRSforTrack (Index document not found) > ({mlTrackId})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePRSforTrack TrackId:{mlTrackId} | Module:{Module}", mlTrackId, "PRS Search");
            }
            return pRSUpdateReturn;
        }

        public async Task<c_tag_extended> AddPPLCTagExtended(CTagExtendedPayload ctagPayload, string isrc)
        {
            try
            {
                List<TagCondition> tagConditions = new List<TagCondition>() {
                new TagCondition()
                {
                    defaultItem = new CTagRulesPayload(){
                        condition = EnTagCondition.Exact,
                        property = "isrc",
                        value = isrc
                    }
                }
                };

                var ctag = new c_tag_extended()
                {
                    condition = JsonConvert.SerializeObject(tagConditions),
                    created_by = Convert.ToInt32(ctagPayload.userId),
                    date_created = DateTime.Now,
                    c_tag_id = ctagPayload.c_tag_id,
                    name = isrc,
                    color = "#ffffff",
                    status = (int)enCtagRuleStatus.Active,
                    date_last_edited = DateTime.Now,
                    last_edited_by = Convert.ToInt32(ctagPayload.userId),
                    track_id = ctagPayload.track_id
                };
                return await _unitOfWork.CTagsExtended.SaveCtagExtended(ctag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddPPLCTagExtended > " + ctagPayload?.track_id);
                return null;
            }
        }        
    }
}
