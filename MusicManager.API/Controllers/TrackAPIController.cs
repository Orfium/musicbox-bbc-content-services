using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elasticsearch.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Helper;
using MusicManager.Logics.Extensions;
using MusicManager.Logics.ServiceLogics;
using Nest;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Soundmouse.Messaging.Model;
using ILogger = Serilog.ILogger;
using System.IO;


namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackAPIController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TrackAPIController> _logger;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IMlMasterTrackLogic _mlMasterTrackLogic;
        private readonly IUploaderLogic _uploaderLogic;
        private readonly IActionLoggerLogic _actionLoggerLogic;
        private readonly IAlbumLogic _albumLogic;
        private readonly ISearchAPIRepository _searchAPIRepository;
        private readonly IElasticLogic _elasticLogic;
        private readonly ILogger _log = Log.ForContext<WeatherForecastController>();

        public TrackAPIController(
            IUnitOfWork unitOfWork,
            ILogger<TrackAPIController> logger,
            IOptions<AppSettings> appSettings,
            IMlMasterTrackLogic mlMasterTrackLogic,
            IUploaderLogic uploaderLogic,
            IElasticLogic elasticLogic, 
            IActionLoggerLogic actionLoggerLogic,
            IAlbumLogic albumLogic,
            ISearchAPIRepository searchAPIRepository)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _appSettings = appSettings;
            _mlMasterTrackLogic = mlMasterTrackLogic;
            _uploaderLogic = uploaderLogic;
            _elasticLogic = elasticLogic;
            _actionLoggerLogic = actionLoggerLogic;
            _albumLogic = albumLogic;
            _searchAPIRepository = searchAPIRepository;
        }

        [HttpPost("CreateNewTrack")]
        public async Task<IActionResult> CreateNewTrack(TrackCreatePayload trackCreatePayload)
        {
            if (trackCreatePayload.trackData == null || string.IsNullOrWhiteSpace(trackCreatePayload.userId) || string.IsNullOrWhiteSpace(trackCreatePayload.orgId))
                return BadRequest("trackData, userId and orgId are required");

            if (trackCreatePayload.trackData.recLabel != null
                && !trackCreatePayload.trackData.recLabel.ToString().Contains("["))
            {
                trackCreatePayload.trackData.rec_label = trackCreatePayload.trackData.recLabel.ToString();
            }            

            MLTrackDocument mLTrackDocument = await _uploaderLogic.CreateNewTrack(trackCreatePayload);
            if (mLTrackDocument == null)
            {               
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation("Add custom track | Object: {@Object}, Module:{Module}", trackCreatePayload, "Create Track");
            return Ok(mLTrackDocument);
        }

        [HttpPost("ProcessTrackXml")]
        public async Task<IActionResult> ProcessTrackXml(TrackXMLPayload trackXMLPayload)
        {
            UploadObject uploadObject = await _uploaderLogic.ProcessUploaderFiles(trackXMLPayload);
            return Ok(uploadObject);
        }


        [HttpPost("GetElasticTrackById")]
        public async Task<IActionResult> GetElasticTrackById(Guid trackid)
        {
            MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocById(trackid);
            return Ok(mLTrackDocument);
        }

        [HttpPost("GetTracksCount")]
        public async Task<IActionResult> GetTracksCount(TrackCountPayload trackCountPayload)
        {
            CountSummary countSummary = new CountSummary();

            if (string.IsNullOrEmpty(trackCountPayload.refId) || trackCountPayload.refId.Length != 36) {
                _logger.LogWarning("GetTracksCount > Invalid ref id", trackCountPayload);
                return Ok(countSummary);
            }                 
           
            try
            {
                c_tag_index_status cTagIndexStatus = await _unitOfWork.CTags.GetCtagIndexStatusByType("indexed");

                workspace_org workspaceOrg = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(Guid.Parse(trackCountPayload.refId), trackCountPayload.orgId);

                countSummary.validIndexCount = await _elasticLogic.GetValidIndexCount(trackCountPayload);
                countSummary.archiveIndexCount = await _elasticLogic.GetArchiveIndexCount(trackCountPayload);
                countSummary.restrictIndexCount = await _elasticLogic.GetRestrictIndexCount(trackCountPayload);
                countSummary.restrictAlbumIndexCount = await _elasticLogic.GetRestrictAlbumIndexCount(trackCountPayload);
                countSummary.sourceDeletedIndexCount = await _elasticLogic.GetSourceIndexCount(trackCountPayload);
                countSummary.sourceDeleteAlbumIndexCount = await _elasticLogic.GetSourceDeletedAlbumIndexCount(trackCountPayload);
                countSummary.ArchivedAlbumIndexCount = await _elasticLogic.GetArchiveAlbumIndexCount(trackCountPayload);
                countSummary.validAlbumIndexCount = await _elasticLogic.GetValidAlbumIndexCount(trackCountPayload);
                countSummary.masterTrackCount = await _unitOfWork.MLMasterTrack.GetMasterTracksCountByWorkspaceId(Guid.Parse(trackCountPayload.refId));
                countSummary.masterAlbumCount = await _unitOfWork.Album.GetMasterAlbumCountByWsId(Guid.Parse(trackCountPayload.refId));
                countSummary.prsIndexedCount = await _elasticLogic.GetPRSIndexedCount(trackCountPayload);
                countSummary.prsNotMatchedCount = await _elasticLogic.GetPRSNotMatchedCount(trackCountPayload);
                if (workspaceOrg != null)
                {
                    countSummary.orgTrackCount = await _unitOfWork.TrackOrg.GetTrackOrgCountByWsOrgId(workspaceOrg.org_workspace_id);
                    countSummary.orgAlbumCount = await _unitOfWork.Album.GetOrgAlbumCountByWsOrgId(workspaceOrg.org_workspace_id);
                }

                if (cTagIndexStatus != null)
                    countSummary.indexedCtagCount = await _elasticLogic.GetIndexCtagCompletedCount(trackCountPayload, cTagIndexStatus);

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetTracksCount {@Payload}",trackCountPayload);
            }
            return Ok(countSummary);
        }

        [HttpPost("UpdateTrackAndAlbum")]
        public async Task<IActionResult> UpdateTrackAndAlbum(TrackUpdatePayload trackUpdatePayload)
        {
            try
            {
                if (trackUpdatePayload.trackData != null && !string.IsNullOrEmpty(trackUpdatePayload.trackData.id))
                {
                    upload_track upload_Track = await _unitOfWork.UploadTrack.FirstOrDefualt(a => a.id == new Guid(trackUpdatePayload.trackData.id));
                    if (upload_Track != null)
                    {
                        upload_Track.metadata_json = JsonConvert.SerializeObject(trackUpdatePayload.trackData, new JsonSerializerSettings());
                        upload_Track.date_last_edited = DateTime.Now;
                        upload_Track.last_edited_by = int.Parse(trackUpdatePayload.userId);
                        upload_Track.modified = true;

                        if (trackUpdatePayload.isAlbumEdit && trackUpdatePayload.albumdata != null)
                        {
                            if (upload_Track.ml_album_id != null)
                            {
                                upload_album upload_Album = await _unitOfWork.UploadAlbum.FirstOrDefualt(a => a.id == upload_Track.ml_album_id);
                                if (upload_Album != null)
                                {
                                    upload_Album.metadata_json = JsonConvert.SerializeObject(trackUpdatePayload.albumdata, new JsonSerializerSettings());
                                    upload_Album.date_last_edited = DateTime.Now;
                                    upload_Album.last_edited_by = int.Parse(trackUpdatePayload.userId);
                                    upload_Album.modified = true;
                                    _unitOfWork.UploadAlbum.Update(upload_Album);
                                }
                            }
                            else
                            {
                                Guid mlAlbumId = Guid.NewGuid();
                                await _unitOfWork.UploadAlbum.Add(new upload_album()
                                {
                                    album_name = trackUpdatePayload.albumdata.album_title,
                                    artist = trackUpdatePayload.albumdata.album_artist,
                                    artwork_uploaded = false,
                                    catalogue_number = trackUpdatePayload.albumdata.catalogue_number,
                                    id = mlAlbumId,
                                    created_by = int.Parse(trackUpdatePayload.userId),
                                    date_created = DateTime.Now,
                                    metadata_json = JsonConvert.SerializeObject(trackUpdatePayload.albumdata, new JsonSerializerSettings()),
                                    modified = true,
                                    session_id = upload_Track.session_id
                                });
                                upload_Track.ml_album_id = mlAlbumId;
                            }
                        }

                        _unitOfWork.UploadTrack.Update(upload_Track);
                        await _unitOfWork.Complete();
                    }

                    return Ok(trackUpdatePayload);
                }
                else
                {
                    return StatusCode(204);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateTrackAndAlbum {@Payload}",trackUpdatePayload);
                return StatusCode(500);
            }
        }

        [HttpPost("UpdateDHTrack")]
        public async Task<IActionResult> UpdateDHTrack(UpdateDHTrackPayload updateDHTrackPayload)
        {
            try
            {
                if (updateDHTrackPayload.trackMetadata.contributor != null)
                {
                    List<Contributor> list = new List<Contributor>();
                    foreach (var item in updateDHTrackPayload.trackMetadata.contributor)
                    {                      
                        //var regex = @"\d{15,15}[\dx]$";
                        //var match = Regex.Match(item.Isni, regex, RegexOptions.IgnoreCase);
                        Contributor contributor = new Contributor()
                        {
                            Name = item.Name,
                            Role = item.Role
                        };
                        list.Add(contributor);
                    }
                    updateDHTrackPayload.trackMetadata.contributor = list;
                }

                Guid? mlVersionId = Guid.NewGuid();

                string oldrecordLabel = "";

                org_user org_User = await _unitOfWork.User.GetUserById(int.Parse(updateDHTrackPayload.userId));
                if (org_User == null)
                    org_User = new org_user() { user_id = int.Parse(updateDHTrackPayload.userId) };

                ml_master_track mlMasterTrack = await _unitOfWork.MLMasterTrack.GetMaterTrackById((Guid)updateDHTrackPayload.trackMetadata.dhTrackId);

                if (mlMasterTrack == null)
                    return Ok(false);

                ml_master_album mlMasterAlbum = null;
                updateDHTrackPayload.trackMetadata.version_id = mlVersionId;

                Track _trackDoc = JsonConvert.DeserializeObject<Track>(mlMasterTrack.metadata);

                if (mlMasterAlbum != null) //--- Update Product details from ml_master_album metadata
                {
                    _trackDoc.TrackData.Product = JsonConvert.DeserializeObject<Product>(mlMasterAlbum.metadata);
                    updateDHTrackPayload.dHAlbum = CommonHelper.CreateDHAlbumFromProduct(_trackDoc.TrackData.Product, enMLUploadAction.ML_ALBUM_ADD.ToString(), mlMasterTrack.library_id, null);
                }

                oldrecordLabel = updateDHTrackPayload.dHTrack.interestedParties.GetRecordLabel();

                //Upload Artwork
                if (updateDHTrackPayload.isAlbumEdit)
                {
                    updateDHTrackPayload.albumMetadata.version_id = mlVersionId;
                    mlMasterAlbum = await _unitOfWork.Album.GetMlMasterAlbumById((Guid)mlMasterTrack.album_id);

                    if (!string.IsNullOrEmpty(updateDHTrackPayload.trackMetadata.artwork_url))
                    {
                        string artworkUrl = await _unitOfWork.UploadAlbum.UploadArtWork(updateDHTrackPayload.albumMetadata.dh_album_id, updateDHTrackPayload.trackMetadata.artwork_url);
                        artworkUrl = artworkUrl?.Replace("&amp;", "&");
                        updateDHTrackPayload.albumMetadata.artwork_url = artworkUrl;
                        updateDHTrackPayload.trackMetadata.artwork_url = artworkUrl;
                        var tracks = await _unitOfWork.UploadTrack.GetTracksFromAlbumId((Guid)updateDHTrackPayload.albumMetadata.id);
                        foreach (var track in tracks)
                        {
                            EditTrackMetadata trackMetadata = JsonConvert.DeserializeObject<EditTrackMetadata>(track.metadata_json);
                            trackMetadata.artwork_url = artworkUrl;
                            track.metadata_json = JsonConvert.SerializeObject(trackMetadata, new JsonSerializerSettings());

                            await _unitOfWork.UploadTrack.UpdateTrackDhAlbumMetaData(track);
                        }
                    }
                }

                updateDHTrackPayload.dHTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, mlMasterTrack.ext_sys_ref);

                //--- Update DHTrack and DHAlbum from edited metadata
                updateDHTrackPayload = _unitOfWork.MLMasterTrack.UpdateEditedTrackObjects(updateDHTrackPayload);

                bool dhSynced = await _unitOfWork.UploadTrack.CheckAndUpdateWhenEdit(updateDHTrackPayload);

                if (updateDHTrackPayload.trackMetadata.dhTrackId != null)
                {
                    long updatedEpochTime = CommonHelper.GetCurrentUtcEpochTimeMicroseconds() - 30000000;

                    //--- If updateDHTrackPayload.dHTrack.id is available this track already been uploaded to DH 
                    if (updateDHTrackPayload.isAlbumEdit)
                    {
                        //--- Update Datahub
                        _ = Task.Run(() => _unitOfWork.MusicAPI.UpdateAlbum(updateDHTrackPayload.dHAlbum.id.ToString(), updateDHTrackPayload.dHAlbum)).ConfigureAwait(false);
                        //--- Update Album Org data
                        _ = Task.Run(() => _albumLogic.UpdateAlbumOrgWhenEdit(updateDHTrackPayload.albumMetadata, org_User, Guid.Parse(updateDHTrackPayload.trackMetadata.id))).ConfigureAwait(false);
                    }

                    //--- Update Datahub
                    _ = Task.Run(() => _unitOfWork.MusicAPI.UpdateTrack(updateDHTrackPayload.trackMetadata.dhTrackId.ToString(), updateDHTrackPayload.dHTrack)).ConfigureAwait(false);

                    MLTrackDocument mLTrackDocument = await _uploaderLogic.UpdateMasterTrackAndAlbumFromUploads((Guid)updateDHTrackPayload.trackMetadata.dhTrackId, _trackDoc,
                    updateDHTrackPayload.trackMetadata,
                    updateDHTrackPayload.albumMetadata,
                    updateDHTrackPayload.isAlbumEdit, updatedEpochTime, !string.IsNullOrEmpty(updateDHTrackPayload.trackMetadata.prs_tune_code) ? updateDHTrackPayload.trackMetadata.prs_tune_code : null);
                    await _elasticLogic.UpdateIndex(mLTrackDocument);

                    //Update Ctag Details of track_org
                    track_org trackOrg = await _unitOfWork.TrackOrg.GetTrackOrgByDhTrackIdAndOrgId((Guid)mLTrackDocument.dhTrackId, updateDHTrackPayload.orgId);                    

                    List<TrackChangeLog> trackChange = trackOrg.change_log == null ? new List<TrackChangeLog>() : JsonConvert.DeserializeObject<List<TrackChangeLog>>(trackOrg.change_log);
                    trackChange.Add(new TrackChangeLog()
                    {
                        Action = enTrackChangeLogAction.EDIT.ToString(),
                        DateCreated = DateTime.Now,
                        UserId = int.Parse(updateDHTrackPayload.userId),
                        UserName = org_User.first_name + " " + org_User.last_name
                    });

                    if (!string.IsNullOrWhiteSpace(updateDHTrackPayload.trackMetadata.sub_origin))
                    {
                        int indexKey = updateDHTrackPayload.trackMetadata.orgTags.FindIndex(a => a.Type == enAdminTypes.SUB_ORIGIN.ToString());
                        if (indexKey < 0)
                        {
                            updateDHTrackPayload.trackMetadata.orgTags.Add(new Tag()
                            {
                                Type = enAdminTypes.SUB_ORIGIN.ToString(),
                                Value = updateDHTrackPayload.trackMetadata.sub_origin
                            });
                        }
                        else
                        {
                            updateDHTrackPayload.trackMetadata.orgTags[indexKey].Value = updateDHTrackPayload.trackMetadata.sub_origin;
                        }
                    }

                    trackOrg.change_log = JsonConvert.SerializeObject(trackChange);
                    trackOrg.org_data = JsonConvert.SerializeObject(updateDHTrackPayload.trackMetadata.orgTags, new JsonSerializerSettings());                 

                    //--- Update Track Org data
                    await _unitOfWork.TrackOrg.UpdateOrgData(trackOrg);                   

                    //--- If the record label is changed update all album tracks
                    if (mlMasterTrack.album_id != null && oldrecordLabel != updateDHTrackPayload.trackMetadata.rec_label)
                    {
                        _ = Task.Run(() => _albumLogic.UpdateRecordLabelOfAllAlbumTracks((Guid)mlMasterTrack.album_id, updateDHTrackPayload.trackMetadata.rec_label, mlMasterTrack.track_id, updateDHTrackPayload.orgId, updateDHTrackPayload.albumMetadata)).ConfigureAwait(false);                        
                    }

                    _logger.LogInformation("Update Track |  TrackId: {TrackId} , UserId: {UserId}, Track Metadata: {@TrackMetadata},Album Metadata: {@AlbumMetadata} , Module: {Module}", 
                        updateDHTrackPayload.dHTrack.id, updateDHTrackPayload.userId, updateDHTrackPayload.trackMetadata, updateDHTrackPayload.albumMetadata, "Track Edit");
                   
                    return Ok(updateDHTrackPayload);
                }
                else
                {
                    return Ok(updateDHTrackPayload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateDHTrack {@Log}", updateDHTrackPayload);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("GetTrackForEdit")]
        public async Task<IActionResult> GetTrackForEdit(TrackEditDeletePayload trackEditDeletePayload)
        {
            try
            {
                DHTrackEdit dHTrackEdit = new DHTrackEdit();

                if (trackEditDeletePayload.workspaceId == _appSettings.Value.MasterWSId)
                {
                    dHTrackEdit.wsType = enWorkspaceType.Master.ToString();
                }
                else
                {
                    enWorkspaceType _type = await _unitOfWork.Workspace.GetWorkspaceType(trackEditDeletePayload.workspaceId, trackEditDeletePayload.orgId);
                    if (_type == enWorkspaceType.External) // If it is External WS can't edit
                    {
                        dHTrackEdit.wsType = enWorkspaceType.External.ToString();
                        return Ok(dHTrackEdit);
                    }
                    else
                    {
                        dHTrackEdit.wsType = _type.ToString();
                    }
                }

                //--- Edit right after upload
                if (trackEditDeletePayload.dhTrackId == null && 
                    dHTrackEdit.wsType == enWorkspaceType.Master.ToString()) {
                    var uploadTrack = await _unitOfWork.UploadTrack.GetUploadTrackByUploadId(trackEditDeletePayload.track_id);
                    if (uploadTrack?.dh_track_id != null)
                        trackEditDeletePayload.dhTrackId = (Guid)uploadTrack?.dh_track_id;
                }
                //--- ------------------------

                ml_master_track mlMasterTrack = null;
                ml_master_album mlMasterAlbum = null;
                track_org track_Org = await _unitOfWork.TrackOrg.GetTrackOrgByDhTrackIdAndOrgId((Guid)trackEditDeletePayload.dhTrackId, trackEditDeletePayload.orgId);
                album_org album_Org = null;

                mlMasterTrack = await _unitOfWork.MLMasterTrack.GetMaterTrackById(track_Org.original_track_id);

                if (mlMasterTrack.album_id != null)
                {
                    album_Org = await _unitOfWork.Album.GetAlbumOrgByDhAlbumIdAndOrgId((Guid)mlMasterTrack.album_id, trackEditDeletePayload.orgId);
                    mlMasterAlbum = await _unitOfWork.Album.GetMlMasterAlbumById((Guid)mlMasterTrack.album_id);
                }

                dHTrackEdit = await _unitOfWork.MLMasterTrack.GetTrackForEdit(trackEditDeletePayload, dHTrackEdit.wsType, mlMasterTrack, mlMasterAlbum, track_Org, album_Org);

                return Ok(dHTrackEdit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTrackForEdit {@EditPayload}", trackEditDeletePayload);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("DeleteTrackBulk")]
        public async Task<IActionResult> DeleteTrackBulk(DeleteTrackPayload trackEditDeletePayload)
        {
            if (trackEditDeletePayload.workspaceId != null)
            {
                enWorkspaceType enWorkspaceType = await _unitOfWork.Workspace.GetWorkspaceType(trackEditDeletePayload.workspaceId, trackEditDeletePayload.orgId);
                if (enWorkspaceType == enWorkspaceType.External)
                    return Ok(-2);
            }

            int status = 0;

            if (trackEditDeletePayload.track_ids?.Count() > 0)
            {
                IEnumerable<Guid> _uniqueAlbumIds = await _unitOfWork.UploadTrack.GetUniqueAlbumIdsByTrackIds(trackEditDeletePayload.track_ids);

                status = await _uploaderLogic.DeleteTrackBulk(trackEditDeletePayload);

                if (_uniqueAlbumIds.Count() > 0) // Clear empty albums
                    _ = Task.Run(() => _albumLogic.ClearEmptyUploadAlbums(_uniqueAlbumIds)).ConfigureAwait(false);
            }

            return Ok(status);
        }

        [HttpPost("DeleteUploadTrack")]
        public async Task<IActionResult> DeleteUploadTrack(DeleteTrackPayload trackEditDeletePayload)
        {
            try
            {
                int deleteState = 0;

                if (trackEditDeletePayload.workspaceId == null)
                {
                    trackEditDeletePayload.workspaceId = _appSettings.Value.MasterWSId;
                }

                foreach (var trackId in trackEditDeletePayload.track_ids)
                {
                    ml_master_track ml_Master_Track = await _unitOfWork.MLMasterTrack.GetMaterTrackByMlId(Guid.Parse(trackId), trackEditDeletePayload.orgId);
                    if (ml_Master_Track != null)
                    {
                        trackEditDeletePayload.dhTrackId = ml_Master_Track.track_id;
                        enWorkspaceType enWorkspaceType = await _unitOfWork.Workspace.GetWorkspaceType(trackEditDeletePayload.workspaceId, trackEditDeletePayload.orgId);

                        deleteState = await _unitOfWork.MLMasterTrack.RemoveTrack(trackEditDeletePayload, enWorkspaceType);
                        if (deleteState >= 0)
                        {
                            await _elasticLogic.DeleteTracks(new Guid[1] { Guid.Parse(trackId) });
                        }
                    }
                    else
                    {
                        deleteState = await _unitOfWork.UploadTrack.RemoveUploadTrackByUploadId(Guid.Parse(trackId));
                        if (deleteState >= 0)
                        {
                            await _elasticLogic.DeleteTracks(new Guid[1] { Guid.Parse(trackId) });
                        }
                    }
                }
                return Ok(deleteState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteUploadTrack {@Payload}",trackEditDeletePayload);
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("UploadAlbumArtwork")]
        public async Task<IActionResult> UploadAlbumArtwork(AlbumArtPayload albumArtPayload)
        {            
            try
            {
                if (albumArtPayload._stream != null)
                {
                    byte[] _ArtworkBytes = Convert.FromBase64String(albumArtPayload._stream.Split(',')[1]);
                    HttpStatusCode httpStatusCode = await _unitOfWork.MusicAPI.UploadArtwork(albumArtPayload.albumId.ToString(), _ArtworkBytes);
                    return Ok(httpStatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadAlbumArtwork {@Payload}",albumArtPayload);
                return BadRequest(ex.Message);
            }
            return Ok();
        }

        [HttpPost("MLCopy")]
        public async Task<IActionResult> MLCopy(MLCopyPayload mLCopyPayload)
        {
            try
            {
                mLCopyPayload.userId = mLCopyPayload.userId;

                EditTrackMetadata editTrackMetadata = await _mlMasterTrackLogic.MakeMLCopy(mLCopyPayload);

                if (editTrackMetadata != null)
                {
                    IActionResult actionResult = await GetTrackForEdit(new TrackEditDeletePayload()
                    {
                        dhTrackId = (Guid)editTrackMetadata.dhTrackId,
                        orgId = mLCopyPayload.orgId,
                        source = "",
                        track_id = Guid.Parse(editTrackMetadata.id),
                        userId = mLCopyPayload.userId,
                        workspaceId = _appSettings.Value.MasterWSId
                    });

                    return actionResult;
                }
                return Ok();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MLCopy {@Payload}",mLCopyPayload);
                return BadRequest(ex.Message);
            }
        } 

        [HttpGet("GetTest")]
        public async Task<IActionResult> GetTest()
        {
            return Ok(Path.GetInvalidFileNameChars());
        }

        [HttpPost("GetTrackById")]
        public async Task<IActionResult> GetTrackById(Guid trackId)
        {
            try
            {
                var track = await _elasticLogic.GetElasticTrackDocById(trackId);
                return Ok(track);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }           
        }

        [HttpPost("ContentAlert")]
        public async Task<IActionResult> ContentAlert(ContentAlertPayLoad contentAlert)
        {
            int result = 0;
            foreach (var item in contentAlert.contentAlerts)
            {
                _logger.LogInformation("ContentAlert {@UserId} : {@Status} : {@Type} : {@Object} , Module: {Module}", contentAlert.userId, result, item.mediaType, item, "Content Alert");

                if (item.mediaType == "TRACK")
                {
                    result = await _unitOfWork.TrackOrg.UpdateTrackContentAlert(true, item, Convert.ToInt32(contentAlert.userId));
                }
                else
                {
                    result = await _unitOfWork.TrackOrg.UpdateAlbumContentAlert(true, item, Convert.ToInt32(contentAlert.userId));
                }
                return Ok(result);
            }
            return Ok(null);
        }


        [HttpPost("ResolveContentAlert")]
        public async Task<IActionResult> ResolveContentAlert(ResolveContentAlertPayLoad contentAlert)
        {
            int result = 0;
            foreach (var item in contentAlert.contentAlerts)
            {
                _logger.LogInformation("ResolveContentAlert {@UserId} : {@Status} : {@Type} : {@Object} , Module: {Module}", contentAlert.userId, result, item.mediaType, item, "Content Alert");

                if (item.mediaType == "TRACK")
                {
                    result = await _unitOfWork.TrackOrg.UpdateResolveTrackContentAlert(false, item, Convert.ToInt32(contentAlert.userId));
                }
                else
                {
                    result = await _unitOfWork.TrackOrg.UpdateResolveAlbumContentAlert(false, item, Convert.ToInt32(contentAlert.userId));
                }
                return Ok(result);
            }
            return Ok(null);
        }

        [HttpPost("CheckDatahubTracks")]
        public async Task<IActionResult> CheckDatahubTracks(CheckDatahubTracksPayload checkDatahubTracksPayload)
        {
            if (checkDatahubTracksPayload.trackIds.Count() == 0)
                return NotFound();

            return Ok(await _searchAPIRepository.CheckDeletedTracks(checkDatahubTracksPayload.trackIds));
        }
    }
}