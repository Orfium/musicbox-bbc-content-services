using Elasticsearch.DataMatching;
using Elasticsearch.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using Newtonsoft.Json;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class DHTrackSync : IDHTrackSync
    {
        private readonly ILogger<DHTrackSync> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IMetadataAPIRepository _metadataAPIRepository;
        private readonly IElasticLogic _elasticLogic;
        private readonly ILogLogic _logLogic;
        private readonly IActionLoggerLogic _actionLoggerLogic;
        private readonly IChartRepository _chartRepository;
        private readonly ICtagLogic _ctagLogic;
        private IEnumerable<string> trackArtists;
        private IEnumerable<string> albumArtists;
        private DateTime updatedDate;
        private DateTime masterAlbumReceivedDate;

        private Guid AppUserId = new Guid("ba19b691-d01d-4b18-82cb-a41ec219f41e");

        public DHTrackSync(ILogger<DHTrackSync> logger,
            IUnitOfWork unitOfWork,
            IOptions<AppSettings> appSettings,
            IMetadataAPIRepository metadataAPIRepository,
            IElasticLogic elasticLogic,
            ILogLogic logLogic,
            IActionLoggerLogic actionLoggerLogic, IChartRepository chartRepository,
            ICtagLogic ctagLogic)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
            _metadataAPIRepository = metadataAPIRepository;
            _elasticLogic = elasticLogic;
            _logLogic = logLogic;
            _actionLoggerLogic = actionLoggerLogic;
            _chartRepository = chartRepository;
            _ctagLogic = ctagLogic;
            updatedDate = DateTime.Now.AddDays(-1);
            masterAlbumReceivedDate = DateTime.Now.AddDays(-1);
        }


        public async Task<int> DownloadDHTracks(workspace workspace, enServiceType _enServiceType)
        {
            int _updatedTrackCount = 0;
            bool _syncStatus = true;
            int _pageSize = 1000;
            int _pageId = 1;
            int _trackCount = 0;
            int _sessionTrackCount = 0;
            bool isPaused = false;

            try
            {
                _logger.LogDebug("Workspace name  - " + workspace.workspace_name);

                TrackAPIResponce trackAPIResponce = new TrackAPIResponce();
                nextPageToken prevPageToken = new nextPageToken();
                List<log_track_api_results> logTrackApiResults = new List<log_track_api_results>();
                log_track_sync_session log_Track_Sync_Session = new log_track_sync_session();

                if (!string.IsNullOrEmpty(workspace.next_page_token))
                {
                    nextPageToken _pageToken = JsonConvert.DeserializeObject<nextPageToken>(workspace.next_page_token);
                    trackAPIResponce.nextPageToken = _pageToken;
                }

                //--- Check until the page results count is less than the page size 
                while (isPaused == false && (trackAPIResponce.results == null || trackAPIResponce.results.Count == _pageSize))
                {
                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)_enServiceType,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = _enServiceType.ToString(),
                        timestamp = DateTime.Now,
                        refId = workspace.workspace_id.ToString()
                    });

                    isPaused = await _unitOfWork.Workspace.CheckPause(workspace.workspace_id);
                    if (!isPaused)
                    {
                        log_Track_Sync_Session = new log_track_sync_session();

                        logTrackApiResults = new List<log_track_api_results>();
                        prevPageToken = trackAPIResponce.nextPageToken;

                        trackAPIResponce = await _unitOfWork.MetadataAPI.GetTrackListByWSId(workspace.workspace_id.ToString(), _pageSize, trackAPIResponce.nextPageToken);                                      

                        if (trackAPIResponce?.results.Count > 0)
                        {
                            _trackCount += trackAPIResponce.results.Count;
                            _sessionTrackCount = trackAPIResponce.results.Count;

                            //---- Save session start
                            log_Track_Sync_Session = new log_track_sync_session()
                            {
                                session_start = DateTime.Now,
                                workspace_id = workspace.workspace_id,
                                synced_tracks_count = 0,
                                download_tracks_count = 0,
                                status = true,
                                page_token = JsonConvert.SerializeObject(prevPageToken, new JsonSerializerSettings())
                            };
                            log_Track_Sync_Session = await _unitOfWork.TrackSyncSession.SaveTrackSyncSession(log_Track_Sync_Session);

                            if (_pageId == 1)
                            {
                                await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { download_status = (int)enLibWSDownloadStatus.Inprogress, workspace_id = workspace.workspace_id });
                            }

                            log_track_api_calls log_Track_Api_Call = new log_track_api_calls()
                            {
                                next_page_token = $"{{\"pageToken\":{JsonConvert.SerializeObject(trackAPIResponce.nextPageToken, new JsonSerializerSettings())}}}",
                                page_size = _pageSize,
                                page_token = $"{{\"pageToken\":{JsonConvert.SerializeObject(prevPageToken, new JsonSerializerSettings())}}}",
                                response_code = 0,
                                track_count = trackAPIResponce.results.Count,
                                ws_id = workspace.workspace_id,
                                session_id = log_Track_Sync_Session.session_id
                            };
                            log_Track_Api_Call = await _unitOfWork.TrackAPICalls.SaveTrackAPICall(log_Track_Api_Call);

                            #region --- Insert track API results to the database  
                            foreach (TrackAPIObj item in trackAPIResponce.results)
                            {
                                logTrackApiResults.Add(new log_track_api_results()
                                {
                                    api_call_id = log_Track_Api_Call.id,
                                    deleted = item.deleted,
                                    metadata = item.value != null ? JsonConvert.SerializeObject(item.value) : null,
                                    workspace_id = item.workspaceId,
                                    track_id = item.trackId,
                                    received = item.received,
                                    version_id = item.versionId,
                                    session_id = log_Track_Sync_Session.session_id,
                                    created_by = AppUserId,
                                    date_created = DateTime.Now
                                });
                            }
                            _updatedTrackCount = await _unitOfWork.TrackAPIResults.BulkInsert(logTrackApiResults);
                            #endregion

                            //--- Check all API results successfully inserted to the database
                            //--- If not request the same page again 
                            if (_sessionTrackCount != _updatedTrackCount)
                            {
                                trackAPIResponce.nextPageToken = prevPageToken;
                                await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { download_status = (int)enLibWSDownloadStatus.DownloadFailed, workspace_id = workspace.workspace_id });
                                _syncStatus = false;
                            }

                            if (_enServiceType != enServiceType.Sync_Master_Workspace)
                                _logger.LogInformation($"Download Session Track Count - {_sessionTrackCount} | Updated Track Count - {_updatedTrackCount} | {workspace.workspace_name}");

                            log_Track_Sync_Session.status = _syncStatus;
                            log_Track_Sync_Session.download_tracks_count = _sessionTrackCount;
                            log_Track_Sync_Session.synced_tracks_count = _updatedTrackCount;
                            log_Track_Sync_Session.download_time = DateTime.Now - log_Track_Sync_Session.session_start;
                            log_Track_Sync_Session.session_end = DateTime.Now;
                            await _unitOfWork.TrackSyncSession.UpdateTrackSyncSession(log_Track_Sync_Session);

                            //--- Update next page token on workspace
                            if (_sessionTrackCount == _updatedTrackCount && trackAPIResponce.nextPageToken != null)
                            {
                                await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { next_page_token = JsonConvert.SerializeObject(trackAPIResponce.nextPageToken, new JsonSerializerSettings()), workspace_id = workspace.workspace_id });
                            }

                            _sessionTrackCount = 0;
                            _updatedTrackCount = 0;
                        }
                    }
                    await _unitOfWork.Workspace.UpdateLastSyncTime(workspace);
                    _pageId++;
                }

                _logger.LogDebug($"{{\"pageToken\":{JsonConvert.SerializeObject(trackAPIResponce.nextPageToken, new JsonSerializerSettings())}}}");

                if (_syncStatus)
                {
                    await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { download_status = (int)enLibWSDownloadStatus.DownloadSuccess, workspace_id = workspace.workspace_id });
                }
                #region --- Update Workspace and Library track counts
                if (_trackCount > 0)
                {
                    _logger.LogDebug("Updating counts - " + DateTime.Now);
                    int _count = await _unitOfWork.Workspace.GetWorkspaceActiveTrackCount(workspace.workspace_id);
                    await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { ml_track_count = _count, workspace_id = workspace.workspace_id });

                    await UpdateLibraryTrackCounts(workspace.workspace_id);

                    _logger.LogDebug("Counts updated - " + DateTime.Now);
                }
                #endregion
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)_enServiceType,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = _enServiceType.ToString(),
                    timestamp = DateTime.Now,
                    refId = workspace.workspace_id.ToString()
                });

                _logger.LogError(ex, "DownloadDHTracks, workspace id {workspaceId}", workspace.workspace_id);
            }
            return _trackCount;
        }

        public async Task<int> DownloadDHAlbums(workspace workspace, enServiceType _enServiceType)
        {
            int _updatedAlbumCount;
            bool _syncStatus = true;
            int _pageSize = 500;
            int _pageId = 1;
            int _trackCount = 0;
            int _sessionTrackCount;
            bool isPaused = false;            

            try
            {
                _logger.LogDebug("Workspace name  - " + workspace.workspace_name);

                AlbumAPIResponce albumAPIResponce = new AlbumAPIResponce();
                nextPageToken prevPageToken = new nextPageToken();
                List<log_album_api_results> logAlbumApiResults = new List<log_album_api_results>();

                if (!string.IsNullOrEmpty(workspace.album_next_page_token))
                {
                    nextPageToken _pageToken = JsonConvert.DeserializeObject<nextPageToken>(workspace.album_next_page_token);
                    albumAPIResponce.nextPageToken = _pageToken;
                }

                //--- Check until the page results count is less than the page size
                while (isPaused == false && (albumAPIResponce.results == null || albumAPIResponce.results.Count == _pageSize))
                {
                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)_enServiceType,
                        status = "pass",
                        serviceName = _enServiceType.ToString(),
                        timestamp = DateTime.Now,
                        refId = workspace.workspace_id.ToString()
                    });

                    isPaused = await _unitOfWork.Workspace.CheckPause(workspace.workspace_id);
                    if (!isPaused)
                    {
                        log_album_sync_session log_Album_Sync_Session = new log_album_sync_session();

                        logAlbumApiResults = new List<log_album_api_results>();
                        prevPageToken = albumAPIResponce.nextPageToken;
                        _logger.LogDebug("Requesting page (album) - " + _pageId);

                        albumAPIResponce = await _unitOfWork.MetadataAPI.GetAlbumListByWSId(workspace.workspace_id.ToString(), _pageSize, albumAPIResponce.nextPageToken);

                        _logger.LogDebug("Responce record count (album) - " + albumAPIResponce.results.Count);

                        if (albumAPIResponce?.results.Count > 0)
                        {                           
                            _trackCount += albumAPIResponce.results.Count;
                            _sessionTrackCount = albumAPIResponce.results.Count;

                            //---- Save session start
                            log_Album_Sync_Session = new log_album_sync_session()
                            {
                                session_start = DateTime.Now,
                                workspace_id = workspace.workspace_id,
                                synced_tracks_count = 0,
                                download_tracks_count = 0,
                                status = true,
                                page_token = JsonConvert.SerializeObject(prevPageToken, new JsonSerializerSettings())
                            };
                            log_Album_Sync_Session = await _unitOfWork.AlbumSyncSession.SaveAlbumSyncSession(log_Album_Sync_Session);

                            if (_pageId == 1)
                            {
                                await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { download_status = (int)enLibWSDownloadStatus.Inprogress, workspace_id = workspace.workspace_id });
                            }

                            log_album_api_calls apiCall = new log_album_api_calls()
                            {
                                date_created = DateTime.Now,
                                next_page_token = $"{{\"pageToken\":{JsonConvert.SerializeObject(albumAPIResponce.nextPageToken, new JsonSerializerSettings())}}}",
                                page_size = _pageSize,
                                page_token = $"{{\"pageToken\":{JsonConvert.SerializeObject(prevPageToken, new JsonSerializerSettings())}}}",
                                response_code = 0,
                                album_count = albumAPIResponce.results.Count,
                                ws_id = workspace.workspace_id,
                                session_id = (int)log_Album_Sync_Session.session_id
                            };
                            apiCall = await _unitOfWork.AlbumAPICalls.SaveAlbumAPICall(apiCall);                            

                            #region --- Insert album API results to the database 
                            foreach (AlbumAPIObj item in albumAPIResponce.results)
                            {
                                logAlbumApiResults.Add(new log_album_api_results()
                                {
                                    api_call_id = apiCall.id,
                                    deleted = item.deleted,
                                    metadata = item.value != null ? JsonConvert.SerializeObject(item.value) : null,
                                    workspace_id = item.workspaceId,
                                    album_id = item.albumId,
                                    version_id = item.versionId,
                                    session_id = (int)log_Album_Sync_Session.session_id,
                                    created_by = AppUserId,
                                    date_created = DateTime.Now,
                                    date_modified = item.dateModified
                                });
                            }
                            _updatedAlbumCount = await _unitOfWork.AlbumAPIResults.BulkInsert(logAlbumApiResults);
                            #endregion

                            //--- Check all API results successfully inserted to the database
                            //--- If not request the same page again 
                            if (_sessionTrackCount != _updatedAlbumCount)
                            {
                                albumAPIResponce.nextPageToken = prevPageToken;
                                await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { download_status = (int)enLibWSDownloadStatus.DownloadFailed, workspace_id = workspace.workspace_id });
                                _syncStatus = false;
                            }

                            _logger.LogInformation($"Download Session Album Count - {_sessionTrackCount} | Updated Album Count - {_updatedAlbumCount} | {workspace.workspace_name}");

                            log_Album_Sync_Session.status = _syncStatus;
                            log_Album_Sync_Session.download_tracks_count = _sessionTrackCount;
                            log_Album_Sync_Session.synced_tracks_count = _updatedAlbumCount;
                            log_Album_Sync_Session.download_time = DateTime.Now - log_Album_Sync_Session.session_start;
                            log_Album_Sync_Session.session_end = DateTime.Now;
                            await _unitOfWork.AlbumSyncSession.UpdateAlbumSyncSession(log_Album_Sync_Session);

                            //--- Update next page token on workspace
                            if (_sessionTrackCount == _updatedAlbumCount && albumAPIResponce.nextPageToken != null)
                            {
                                await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { album_next_page_token = JsonConvert.SerializeObject(albumAPIResponce.nextPageToken, new JsonSerializerSettings()), workspace_id = workspace.workspace_id });
                            }

                            _sessionTrackCount = 0;
                            _updatedAlbumCount = 0;
                        }
                        await _unitOfWork.Workspace.UpdateLastSyncTime(workspace);
                        _pageId++;
                    }
                }

                _logger.LogDebug($"{{\"pageToken\":{JsonConvert.SerializeObject(albumAPIResponce.nextPageToken, new JsonSerializerSettings())}}}");

                if (_trackCount > 0 && _syncStatus)
                {
                    await _unitOfWork.Workspace.UpdateWorkspace(new workspace() { download_status = (int)enLibWSDownloadStatus.DownloadSuccess, workspace_id = workspace.workspace_id });
                }
                
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)_enServiceType,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = _enServiceType.ToString(),
                    timestamp = DateTime.Now,
                    refId = workspace.workspace_id.ToString()
                });

                _logger.LogError(ex, "DownloadDHAlbums");               
            }
            return _trackCount;
        }


        public async Task<int> ElasticIndex(workspace_org workspace_org, enServiceType _enServiceType)
        {
            int pageIndex = 0;
            int pageSize = 1000;
            var isPaused = await _unitOfWork.Workspace.CheckPause(workspace_org.workspace_id);
            DateTime processTime = DateTime.Now;
            long lastIndexId = 0;
            int indexCount = 0;           

            if (!isPaused)
            {
                IEnumerable<LogElasticTrackChange> searchData = await _unitOfWork.LogElasticTrackChange.Search( pageSize, workspace_org.org_workspace_id);
                DateTime startTime = DateTime.Now;
                string docId = "";
                enIndexStatus status = enIndexStatus.ToBeIndexed;
                int x = 0;

                _logger.LogDebug($"Search time  - {(DateTime.Now - processTime).TotalSeconds}");


                try
                {
                    if (searchData.Count() > 0)
                    {
                        await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                        {
                            org_workspace_id = workspace_org.org_workspace_id,
                            index_status = (int)enIndexStatus.Inprogress
                        });

                        while ((int)searchData.Count() > 0)
                        {
                            await _actionLoggerLogic.ServiceLog(new ServiceLog()
                            {
                                id = (int)_enServiceType,
                                status = enServiceStatus.pass.ToString(),
                                serviceName = _enServiceType.ToString(),
                                timestamp = DateTime.Now,
                                refId = workspace_org.workspace_id.ToString()
                            });

                            List<MLTrackDocument> trackDocuments = new List<MLTrackDocument>();
                            List<MLTrackDocument> DeletedTrackDocuments = new List<MLTrackDocument>();
                            status = enIndexStatus.Inprogress;

                            processTime = DateTime.Now;

                            foreach (LogElasticTrackChange item in searchData)
                            {
                                x++;
                                MLTrackDocument mLTrackDocument = new MLTrackDocument();
                                TrackOrg _trackOrg = JsonConvert.DeserializeObject<TrackOrg>(item.track_org_data);

                                if (_trackOrg.clearance_track)
                                    mLTrackDocument.mlCreated = true;

                                //--- Delete from DH
                                if (item.deleted == true)
                                {
                                    mLTrackDocument.sourceDeleted = true;
                                    mLTrackDocument.archived = true;
                                    mLTrackDocument.takedownDate = DateTime.Now;
                                    mLTrackDocument.takedownType = enTakedownType.DH.ToString();
                                }
                                //--- Archived by ML user
                                else if (item.archived == true)
                                {
                                    mLTrackDocument.archived = true;
                                    mLTrackDocument.takedownDate = DateTime.Now;
                                    mLTrackDocument.takedownType = enTakedownType.USR.ToString();
                                    mLTrackDocument.takedownUser = _trackOrg.last_edited_by;
                                }

                                mLTrackDocument.org_id = item.org_id;

                                if (item.metadata != null)
                                {
                                    Track _trackDoc = JsonConvert.DeserializeObject<Track>(item.metadata);

                                    //--- Track expired
                                    if (item.deleted == false && _trackDoc.Source?.ValidTo != null && _trackDoc.Source?.ValidTo <= DateTime.Now)
                                    {
                                        mLTrackDocument.sourceDeleted = true;
                                        mLTrackDocument.archived = true;
                                        mLTrackDocument.takedownDate = DateTime.Now;
                                        mLTrackDocument.takedownType = enTakedownType.EXP.ToString();
                                    }

                                    mLTrackDocument.preRelease = _trackDoc.Source?.IsPreRelease;                                   

                                    if (_trackDoc.Source?.ValidFrom != null)
                                    {

                                        if (_trackDoc.Source.ValidFrom <= DateTime.Now)
                                        {
                                            mLTrackDocument.searchableFrom = _trackDoc.Source.ValidFrom;
                                            mLTrackDocument.searchableType = "EXP";
                                            mLTrackDocument.preRelease = false;
                                        }
                                        else
                                        {
                                            mLTrackDocument.searchableFrom = null;
                                            mLTrackDocument.preRelease = true;
                                        }
                                    }

                                    if (item.album_id != null && item.album_metadata != null)
                                        _trackDoc.TrackData.Product = JsonConvert.DeserializeObject<Product>(item.album_metadata);

                                    docId = _trackDoc.Id.ToString();

                                    mLTrackDocument = _trackDoc.GenerateMLTrackDocument(item, mLTrackDocument, _trackOrg);
                                }
                                else
                                {
                                    mLTrackDocument.id = item.document_id;
                                }

                                if (mLTrackDocument.restricted != true && item.restricted != null)
                                {
                                    mLTrackDocument.restricted = item.restricted ?? false;
                                }

                                if(mLTrackDocument.mlCreated == true && mLTrackDocument.musicOrigin == enDHMusicOrigin.live.ToString())
                                    mLTrackDocument.liveCopy = true;

                                mLTrackDocument.dateReceived = item.received;
                                mLTrackDocument.oriVersionId = item.dh_version_id;
                                mLTrackDocument.wsName = item.workspace_name;
                                mLTrackDocument.libName = item.library_name;
                                mLTrackDocument.wsType = item.ws_type;
                                mLTrackDocument.extIdentifiers = string.IsNullOrEmpty(item.external_identifiers) ? null : JsonConvert.DeserializeObject<IDictionary<string, string>>(item.external_identifiers);

                                if (workspace_org.music_origin != null && string.IsNullOrEmpty(mLTrackDocument.musicOriginSubIndicator)) //&& mLTrackDocument.musicOrigin == enDHMusicOrigin.library.ToString())
                                {
                                    switch (workspace_org.music_origin)
                                    {
                                        case 2:
                                            mLTrackDocument.musicOriginSubIndicator = "MCPS";
                                            break;

                                        case 3:
                                            mLTrackDocument.musicOriginSubIndicator = "Non-MCPS";
                                            break;
                                    }
                                }

                                //---- Clear temp data if it is a edited track
                                if (item.edit_track_metadata != null)
                                {
                                    await _unitOfWork.MLMasterTrack.UpdateEditeTrackAndAlbumMetadata(new ml_master_track()
                                    {
                                        track_id = (Guid)mLTrackDocument.dhTrackId,
                                        edit_album_metadata = null,
                                        edit_track_metadata = null
                                    });
                                }                              

                                if (item.ws_type == "Master" && mLTrackDocument.dhTrackId != null)
                                {                                   
                                    await _unitOfWork.UploadTrack.UpdateSyncStatus((Guid)mLTrackDocument.dhTrackId, mLTrackDocument.duration);                                    
                                }

                                trackDocuments.Add(mLTrackDocument);
                                lastIndexId = item.id;
                            }

                            if (trackDocuments.Count > 0)
                            {
                                _logger.LogDebug($"Prepare doc time  - {(DateTime.Now - processTime).TotalSeconds}");
                                processTime = DateTime.Now;

                                bool? indexStatus = await BulkIndexTrackDocument(trackDocuments);

                                if (indexStatus == true)
                                {
                                    indexCount += trackDocuments.Count;
                                    processTime = DateTime.Now;

                                    await _unitOfWork.LogElasticTrackChange.BulkDelete(workspace_org.org_workspace_id, lastIndexId);

                                    processTime = DateTime.Now;
                                }
                                else if (indexStatus == false)
                                {
                                    status = enIndexStatus.IndexFailed;
                                    await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                                    {
                                        org_workspace_id = workspace_org.org_workspace_id,
                                        index_status = (int)status
                                    });
                                    break;
                                }
                                else {
                                    status = enIndexStatus.IndexFailed;
                                    await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                                    {
                                        org_workspace_id = workspace_org.org_workspace_id,
                                        index_status = (int)status
                                    });
                                    break;
                                }                                
                            }

                            _logger.LogInformation($"Bulk Track Indexed cout - {x} | {workspace_org.workspace_id}");                          

                            pageIndex++;

                            searchData = await _unitOfWork.LogElasticTrackChange.Search(pageSize, workspace_org.org_workspace_id);

                            _logger.LogDebug($"Index time  - {(DateTime.Now - processTime).TotalSeconds}");
                            processTime = DateTime.Now;

                            if (searchData.Count() <= 0)
                            {
                                status = enIndexStatus.IndexSuccess;
                            }

                            await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                            {
                                org_workspace_id = workspace_org.org_workspace_id,
                                index_status = (int)status
                            });
                        }
                    }
                    _logger.LogDebug($"Indexed cout - {x} / Duration - {(DateTime.Now - startTime)}");
                    _logger.LogDebug($"---------------------------------------------------------------------------------------------");
                }
                catch (Exception ex)
                {
                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)_enServiceType,
                        status = enServiceStatus.fail.ToString(),
                        serviceName = _enServiceType.ToString(),
                        timestamp = DateTime.Now,
                        refId = workspace_org.workspace_id.ToString()
                    });
                    _logger.LogError($"Index Error > ({docId})", ex);                    
                }
            }

            return indexCount;
        }

        public async Task<bool?> BulkIndexTrackDocument(List<MLTrackDocument> trackDocuments)
        {
            var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(trackDocuments);          

            if (asyncIndexResponse == null)
            {
                return null;
            }
            else if (asyncIndexResponse.Length > 0)
            {
                _logger.LogError("BulkIndexTrackDocument Errors - " + asyncIndexResponse[0].id);
                _logLogic.LogErrors(asyncIndexResponse);
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<bool?> BulkIndexAlbumDocument(List<MLAlbumDocument> mLAlbumDocuments, string orgId)
        {
            var asyncIndexResponse = await _elasticLogic.BulkIndexAlbumDocument(mLAlbumDocuments, orgId);

            if (asyncIndexResponse == null)
            {
                return null;
            }
            else if (asyncIndexResponse.Length > 0)
            {
                _logger.LogError("BulkIndexAlbumDocument Errors - " + asyncIndexResponse);
                _logLogic.LogErrors(asyncIndexResponse);
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<int> AlbumElasticIndex(workspace_org workspace_org, enServiceType _enServiceType)
        {
            int pageIndex = 0;
            int pageSize = 200;
            var isPaused = await _unitOfWork.Workspace.CheckPause(workspace_org.workspace_id);
            int indexCount = 0;

            if (!isPaused)
            {
                IEnumerable<LogElasticAlbumChange> searchData =await _unitOfWork.LogElasticTrackChange.SearchElasticAlbumChange( pageSize, workspace_org.org_workspace_id);
                DateTime startTime = DateTime.Now;
                enIndexStatus status = enIndexStatus.ToBeIndexed;
                int x = 0;

                try
                {
                    if (searchData?.Count() > 0)
                    {
                        await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                        {
                            org_workspace_id = workspace_org.org_workspace_id,
                            index_status = (int)enIndexStatus.Inprogress
                        });

                        while (searchData?.Count() > 0)
                        {
                            await _actionLoggerLogic.ServiceLog(new ServiceLog()
                            {
                                id = (int)_enServiceType,
                                status = enServiceStatus.pass.ToString(),
                                serviceName = _enServiceType.ToString(),
                                timestamp = DateTime.Now,
                                refId = workspace_org.workspace_id.ToString()
                            });

                            List<MLAlbumDocument> albumDocuments = new List<MLAlbumDocument>();

                            foreach (LogElasticAlbumChange item in searchData)
                            {
                                x++;
                                SearchData<List<MLTrackDocument>> ml_Master_Tracks = new SearchData<List<MLTrackDocument>>();
                                MLAlbumDocument mLAlbumDocument = new MLAlbumDocument();
                                int trackCount = 0;

                                AlbumOrg _albumOrg = JsonConvert.DeserializeObject<AlbumOrg>(item.album_org_data);
                                ml_Master_Tracks = await _elasticLogic.GetAllMasterTracksByAlbumId(item.album_id, workspace_org.org_id);
                                trackCount = ml_Master_Tracks?.Data?.Count() ?? 0;

                                if (trackCount == 0 && ml_Master_Tracks == null)
                                    trackCount = await _unitOfWork.MLMasterTrack.GetMasterTracksCountByAlbumId(item.album_id, workspace_org.org_id);

                                if (ml_Master_Tracks == null)
                                {
                                    ml_Master_Tracks = new SearchData<List<MLTrackDocument>>()
                                    {
                                        Data = new List<MLTrackDocument>(),
                                        TotalCount = 0
                                    };
                                }

                                if (item.metadata != null)
                                {
                                    Product product = JsonConvert.DeserializeObject<Product>(item.metadata);
                                    mLAlbumDocument = product.GenerateMLAlbumDocument(mLAlbumDocument, ml_Master_Tracks.Data, _albumOrg);
                                    mLAlbumDocument.archived = item.archived ?? false;
                                }
                                else
                                {
                                    mLAlbumDocument.id = item.document_id;
                                    mLAlbumDocument.sourceDeleted = true;
                                    mLAlbumDocument.archived = true;
                                }

                                mLAlbumDocument.wsId = item.workspace_id;
                                mLAlbumDocument.libId = item.library_id;
                                mLAlbumDocument.wsType = item.ws_type;
                                mLAlbumDocument.wsName = item.workspace_name;
                                mLAlbumDocument.libName = item.library_name;
                                mLAlbumDocument.libNotes = item.library_notes;
                                mLAlbumDocument.restricted = item.restricted ?? false;

                                if (workspace_org.music_origin != null) //&& mLTrackDocument.musicOrigin == enDHMusicOrigin.library.ToString())
                                {
                                    switch (workspace_org.music_origin)
                                    {
                                        case 2:
                                            mLAlbumDocument.musicOriginSubIndicator = "MCPS";
                                            break;

                                        case 3:
                                            mLAlbumDocument.musicOriginSubIndicator = "Non-MCPS";
                                            break;
                                    }
                                }

                                if (ml_Master_Tracks.Data?.Count() == 0 && trackCount == 0)
                                {
                                    //mLAlbumDocument.archived = true;
                                }
                                else if (ml_Master_Tracks.Data?.Count() == 0 && trackCount > 0)
                                {
                                    _logger.LogWarning("Elastic track documents not found in Albun index service - Album id - " + item.album_id);
                                }
                                albumDocuments.Add(mLAlbumDocument);
                                _logger.LogDebug($"Album Added - {x}");
                            }

                            if (albumDocuments.Count > 0)
                            {
                                bool? indexStatus = await BulkIndexAlbumDocument(albumDocuments, workspace_org.org_id);

                                if (indexStatus == true)
                                {
                                    indexCount += albumDocuments.Count;
                                    await _unitOfWork.LogElasticTrackChange.BulkDeleteAlbums(albumDocuments);
                                }
                                else if (indexStatus == false)
                                {
                                    status = enIndexStatus.IndexFailed;
                                    await _unitOfWork.LogElasticTrackChange.BulkDeleteAlbums(albumDocuments);
                                    await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                                    {
                                        org_workspace_id = workspace_org.org_workspace_id,
                                        index_status = (int)status
                                    });
                                    break;
                                }
                                else if (indexStatus == false)
                                {
                                    status = enIndexStatus.IndexFailed;
                                    await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                                    {
                                        org_workspace_id = workspace_org.org_workspace_id,
                                        index_status = (int)status
                                    });                                    
                                    break;
                                }
                                else {
                                    status = enIndexStatus.IndexFailed;                                   
                                    await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                                    {
                                        org_workspace_id = workspace_org.org_workspace_id,
                                        index_status = (int)status
                                    });
                                    break;
                                }
                            }

                            _logger.LogInformation($"Bulk album Indexed cout - {x} | {workspace_org.workspace_id}");                           

                            pageIndex++;

                            searchData =await _unitOfWork.LogElasticTrackChange.SearchElasticAlbumChange(pageSize, workspace_org.org_workspace_id);

                            if (searchData.Count() <= 0)
                            {
                                status = enIndexStatus.IndexSuccess;

                                await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org
                                {
                                    org_workspace_id = workspace_org.org_workspace_id,
                                    index_status = (int)status
                                });
                            }
                        }
                    }
                    _logger.LogDebug($"Album Indexed cout - {x} / Duration - {(DateTime.Now - startTime)}");
                }
                catch (Exception ex)
                {
                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)_enServiceType,
                        status = enServiceStatus.fail.ToString(),
                        serviceName = _enServiceType.ToString(),
                        timestamp = DateTime.Now,
                        refId = workspace_org.workspace_id.ToString()
                    });
                    _logger.LogError("AlbumElasticIndex", ex);                    
                }
            }

            return indexCount;
        }
       
        private async Task UpdateLibraryTrackCounts(Guid wsId)
        {
            try
            {
                List<library> libraries = _unitOfWork.Library.Find(a => a.workspace_id == wsId).ToList();
                
                foreach (library item in libraries)
                {
                    int _count = await _unitOfWork.MLMasterTrack.GetMasterTracksCountByLibraryId(item.library_id);
                    item.ml_track_count = _count;
                    await _unitOfWork.Library.UpdateLibraryTrackCount(item);  
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateLibraryTrackCounts | WorkspaceId : {WorkspaceId}", wsId);
            }
        }

        public async Task SyncTracks(workspace_org workspace_Org, enServiceType _enServiceType)
        {
            _logger.LogDebug("-----------------------------------------");
            _logger.LogDebug("WS ID - " + workspace_Org.workspace_id + " - " + DateTime.Now);

            try
            {
                int intUpdateCount = 1000;
                var isPaused = await _unitOfWork.Workspace.CheckPause(workspace_Org.workspace_id);
                if (!isPaused)
                {
                    IEnumerable<library_org> library_Orgs = await _unitOfWork.Library.GetOrgLibraryListByWorkspaceId(workspace_Org.workspace_id, workspace_Org.org_id);
                    library_Orgs = library_Orgs.Where(a => a.ml_status != workspace_Org.ml_status);

                    switch (workspace_Org.ml_status)
                    {
                        case (int)enMLStatus.Live:
                        case (int)enMLStatus.Archive:
                        case (int)enMLStatus.Restrict:
                            if (workspace_Org.sync_status != (int)enSyncStatus.SyncFailed)
                            {
                                await SetLiveArchive((int)workspace_Org.ml_status, workspace_Org.org_workspace_id, enWorkspaceLib.ws, workspace_Org.workspace_id, workspace_Org.last_sync_api_result_id, library_Orgs, intUpdateCount, workspace_Org.last_edited_by, workspace_Org.org_id, workspace_Org.restricted ?? false, workspace_Org.org_workspace_id, _enServiceType);
                                await SetLiveArchiveAlbum((int)workspace_Org.ml_status, workspace_Org.org_workspace_id, enWorkspaceLib.ws, workspace_Org.workspace_id, workspace_Org.last_album_sync_api_result_id, library_Orgs, intUpdateCount, workspace_Org.last_edited_by, workspace_Org.org_id, workspace_Org.restricted ?? false, workspace_Org.org_workspace_id, _enServiceType);
                            }
                            else
                            {
                                _logger.LogDebug("WS ID - Process failed - " + workspace_Org.workspace_id);
                            }
                            break;
                        default:
                            break;
                    }

                    if (library_Orgs.Count() > 0)
                    {
                        foreach (var item in library_Orgs)
                        {
                            switch (item.ml_status)
                            {
                                case (int)enMLStatus.Live:
                                case (int)enMLStatus.Archive:
                                case (int)enMLStatus.Restrict:
                                    await SetLiveArchive(item.ml_status, item.org_library_id, enWorkspaceLib.lib, item.library_id, item.last_sync_api_result_id, library_Orgs, intUpdateCount, item.last_edited_by, workspace_Org.org_id, item.restricted, workspace_Org.org_workspace_id, _enServiceType);
                                    await SetLiveArchiveAlbum(item.ml_status, item.org_library_id, enWorkspaceLib.lib, item.library_id, item.last_album_sync_api_result_id, library_Orgs, intUpdateCount, item.last_edited_by, workspace_Org.org_id, item.restricted, workspace_Org.org_workspace_id, _enServiceType);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task SetLiveArchive(int mlStatus, Guid wsLibOrgId, enWorkspaceLib type, Guid refId, long? lastSyncApiResultId, IEnumerable<library_org> library_Orgs, int limit, int userId, string orgId, bool restricted, Guid orgWorkspaceId, enServiceType _enServiceType)
        {
            int? newStatus = mlStatus;
            int successCount = 0;
            int trackCount = 0;
            int pageNo = 0;
            bool isChartArtist = false;

            try
            {
                if (type == enWorkspaceLib.lib)
                {
                    library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(refId, orgId);

                    if (library_Org != null)
                        lastSyncApiResultId = library_Org.last_sync_api_result_id;
                }

                IEnumerable<ml_master_track> mlMasterTracks = await _unitOfWork.MLMasterTrack.GetMasterTrackListForSetLive(type, refId, lastSyncApiResultId, library_Orgs, limit, orgId, pageNo);

                if (mlMasterTracks?.Count() > 0)
                {

                    if (type == enWorkspaceLib.ws)
                    {
                        await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                        {
                            org_workspace_id = wsLibOrgId,
                            sync_status = (int)enSyncStatus.Inprogress
                        });
                    }
                    else
                    {
                        await _unitOfWork.Library.UpdateLibraryOrg(new library_org()
                        {
                            org_library_id = wsLibOrgId,
                            sync_status = (int)enSyncStatus.Inprogress
                        });
                    }

                    while (mlMasterTracks.Count() > 0)
                    {
                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)_enServiceType,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = _enServiceType.ToString(),
                            timestamp = DateTime.Now,
                            refId = refId.ToString()
                        });

                        trackCount += mlMasterTracks.Count();

                        _logger.LogInformation($"Sync Track count - {mlMasterTracks.Count()} - {refId}");

                        List<track_org> trackOrgs = new List<track_org>();
                        DescriptiveData CopyDescriptiveData = null;
                        DescriptiveData UploadDescriptiveData = null;
                        List<TrackChangeLog> changeLog = null;
                        Guid uploadId;

                        foreach (ml_master_track item in mlMasterTracks)
                        {
                            if (item.track_id.ToString() == "41dceec0-55fd-4f78-a249-2282e008561d") {
                                string aa = "";
                            }
                               

                            isChartArtist = false;
                            uploadId = Guid.NewGuid();
                            CopyDescriptiveData = null;
                            UploadDescriptiveData = null;
                            Track trackDoc = null;


                            if (item.metadata != null)
                            {
                                trackDoc = JsonConvert.DeserializeObject<Track>(item.metadata);
                                //Update track org - chart artist

                                if (updatedDate.Date != DateTime.Now.Date)
                                {
                                    trackArtists = await _chartRepository.GetDistinctTrackArtists();
                                    updatedDate = DateTime.Now;
                                }                                

                                if (trackArtists != null && trackDoc?.TrackData?.InterestedParties?.Count() > 0)
                                {
                                    var performersList = GetNameListByRole("performer", trackDoc.TrackData.InterestedParties);
                                    if (performersList?.Count() > 0)
                                    {
                                        foreach (var performer in performersList)
                                        {
                                            if (trackArtists.Contains(performer.ToLower()))
                                            {
                                                isChartArtist = true;
                                                break;
                                            }
                                            updatedDate = DateTime.Now;
                                        }
                                    }
                                }

                                if (trackDoc.TrackData.DescriptiveExtended != null)
                                {
                                    changeLog = new List<TrackChangeLog>();
                                    UploadDescriptiveData = trackDoc.TrackData.DescriptiveExtended?.SingleOrDefault(a => a.Source == enDescriptiveExtendedSource.ML_UPLOAD.ToString());
                                    CopyDescriptiveData = trackDoc.TrackData.DescriptiveExtended?.SingleOrDefault(a => a.Source == enDescriptiveExtendedSource.ML_COPY.ToString());
                                }

                                if (UploadDescriptiveData != null)
                                {
                                    try
                                    {
                                        TrackChangeLog trackChangeLog = JsonConvert.DeserializeObject<TrackChangeLog>(UploadDescriptiveData.Value.ToString());

                                        if (trackChangeLog != null)
                                        {
                                            if (trackChangeLog.RefId != null)
                                                uploadId = (Guid)trackChangeLog.RefId;

                                            changeLog.Add(trackChangeLog);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "SetLiveArchive > UploadDescriptiveData > DH Track Id : " + item.track_id);                                       
                                    }
                                }

                                if (CopyDescriptiveData != null)
                                {
                                    try
                                    {
                                        TrackChangeLog trackChangeLog = JsonConvert.DeserializeObject<TrackChangeLog>(CopyDescriptiveData.Value.ToString());

                                        if (trackChangeLog != null)
                                        {
                                            if (trackChangeLog.RefId != null)
                                                uploadId = (Guid)trackChangeLog.RefId;

                                            changeLog.Add(trackChangeLog);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "SetLiveArchive > CopyDescriptiveData > DH Track Id : " + item.track_id);                                        
                                    }
                                }
                            }                            

                            trackOrgs.Add(new track_org()
                            {
                                id = uploadId,
                                original_track_id = item.track_id,
                                album_id = trackDoc?.TrackData?.Product?.Id ?? null,
                                archive = false,
                                created_by = userId,
                                last_edited_by = userId,
                                source_deleted = item.deleted,
                                ml_status = mlStatus,
                                org_id = orgId,
                                restricted = restricted,
                                org_workspace_id = orgWorkspaceId,
                                api_result_id = item.api_result_id,
                                change_log = changeLog == null ? null : JsonConvert.SerializeObject(changeLog, new JsonSerializerSettings()),
                                chart_artist = isChartArtist                                
                            });
                            lastSyncApiResultId = item.api_result_id;
                        }
                        if (trackOrgs.Count() > 0)
                            successCount += await _unitOfWork.TrackOrg.InsertUpdateTrackOrg(trackOrgs);

                        //--- Check whether ml status has been changed
                        if (type == enWorkspaceLib.lib)
                        {
                            library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(refId, orgId);
                            newStatus = library_Org?.ml_status;
                        }
                        else
                        {
                            workspace_org workspace_Org = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(refId, orgId);
                            newStatus = workspace_Org?.ml_status;
                        }

                        //lastSyncApiResultId = mlMasterTracks.LastOrDefault().api_result_id;

                        if (trackCount == successCount)
                        {
                            if (type == enWorkspaceLib.ws)
                            {
                                await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                                {
                                    last_sync_api_result_id = newStatus == mlStatus ? lastSyncApiResultId : 0,
                                    workspace_id = refId,
                                    org_id = orgId
                                });
                            }
                            else
                            {
                                await _unitOfWork.Library.UpdateLibraryOrg(new library_org()
                                {
                                    last_sync_api_result_id = newStatus == mlStatus ? lastSyncApiResultId : 0,
                                    library_id = refId,
                                    org_id = orgId
                                });
                            }
                        }

                        //last_sync_date_time = syncDateTime;
                        if (newStatus == mlStatus)
                        {
                            pageNo++;
                            mlMasterTracks = await _unitOfWork.MLMasterTrack.GetMasterTrackListForSetLive(type, refId, lastSyncApiResultId, library_Orgs, limit, orgId, pageNo);
                        }
                        else
                        {
                            mlMasterTracks = null;
                        }

                        _logger.LogDebug(successCount + " - Tracks are succesfully synced");
                    }

                    if (type == enWorkspaceLib.ws)
                    {
                        workspace workspace = await _unitOfWork.Workspace.GetWorkspaceById(refId);

                        if (workspace.download_status == (int)enLibWSDownloadStatus.DownloadSuccess ||
                            workspace.download_status == (int)enLibWSDownloadStatus.DownloadFailed)
                        {
                            await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                            {
                                org_workspace_id = wsLibOrgId,
                                sync_status = trackCount == successCount ? (int)enSyncStatus.SyncSuccess : (int)enSyncStatus.SyncFailed
                            });
                        }
                    }
                    else
                    {
                        await _unitOfWork.Library.UpdateLibraryOrg(new library_org()
                        {
                            org_library_id = wsLibOrgId,
                            sync_status = trackCount == successCount ? (int)enSyncStatus.SyncSuccess : (int)enSyncStatus.SyncFailed
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)_enServiceType,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = _enServiceType.ToString(),
                    timestamp = DateTime.Now,
                    refId = refId.ToString()
                });
                _logger.LogError(ex, "SetLiveArchive");
                throw;
            }
        }

        private async Task SetLiveArchiveAlbum(int mlStatus, Guid wsLibOrgId, enWorkspaceLib type, Guid refId, long? lastSyncApiResultId, IEnumerable<library_org> library_Orgs, int limit, int userId, string orgId, bool restricted, Guid orgWorkspaceId, enServiceType _enServiceType)
        {
            int? newStatus = mlStatus;
            int successCount = 0;
            int trackCount = 0;
            bool isChartArtist = false;

            try
            {
                if (type == enWorkspaceLib.lib)
                {
                    library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(refId, orgId);
                    lastSyncApiResultId = (long)library_Org?.last_album_sync_api_result_id;
                }

                IEnumerable<ml_master_album> mlMasterAlbums = await _unitOfWork.MLMasterTrack.GetMasterAlbumListForSetLive(type, refId, lastSyncApiResultId, library_Orgs, limit);

                if (mlMasterAlbums?.Count() > 0)
                {

                    if (type == enWorkspaceLib.ws)
                    {
                        await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                        {
                            org_workspace_id = wsLibOrgId,
                            album_sync_status = (int)enSyncStatus.Inprogress
                        });
                    }
                    else
                    {
                        await _unitOfWork.Library.UpdateLibraryOrg(new library_org()
                        {
                            org_library_id = wsLibOrgId,
                            album_sync_status = (int)enSyncStatus.Inprogress
                        });
                    }

                    while (mlMasterAlbums.Count() > 0)
                    {                      

                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)_enServiceType,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = _enServiceType.ToString(),
                            timestamp = DateTime.Now,
                            refId = refId.ToString()
                        });

                        trackCount += mlMasterAlbums.Count();

                        _logger.LogInformation($"Sync Album count - {mlMasterAlbums.Count()} - {refId}");                        

                        List<album_org> albumOrgs = new List<album_org>();
                        Guid uploadId;

                        foreach (ml_master_album item in mlMasterAlbums)
                        {
                            isChartArtist = false;
                            uploadId = Guid.NewGuid();
                            DescriptiveData CopyDescriptiveData = null;
                            DescriptiveData UploadDescriptiveData = null;
                            List<TrackChangeLog> changeLog = null;

                            Product product = null;                          

                            try
                            {
                                if (item.metadata != null)
                                {
                                    product = JsonConvert.DeserializeObject<Product>(item.metadata);

                                    //Update track org - chart artist
                                    if (masterAlbumReceivedDate != null)
                                    {
                                        if (masterAlbumReceivedDate.Date != DateTime.Now.Date)
                                        {
                                            albumArtists = await _chartRepository.GetDistinctAlbumArtists();                                           
                                            masterAlbumReceivedDate = DateTime.Now;
                                        }
                                    }

                                    if (albumArtists != null)
                                    {                                       

                                        if (albumArtists.Contains(product.Artist?.ToLower()))
                                        {                                                                                 
                                            isChartArtist = true;                                            
                                        }
                                        masterAlbumReceivedDate = DateTime.Now;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "SetLiveArchiveAlbum > DeserializeObject > DH Album Id : " + item.album_id);

                                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                                {
                                    id = (int)_enServiceType,
                                    status = enServiceStatus.fail.ToString(),
                                    serviceName = _enServiceType.ToString(),
                                    timestamp = DateTime.Now,
                                    refId = refId.ToString()
                                });
                            }


                            if (product?.DescriptiveExtended != null)
                            {
                                changeLog = new List<TrackChangeLog>();
                                UploadDescriptiveData = product.DescriptiveExtended?.SingleOrDefault(a => a.Source == enDescriptiveExtendedSource.ML_UPLOAD.ToString());
                                CopyDescriptiveData = product.DescriptiveExtended?.SingleOrDefault(a => a.Source == enDescriptiveExtendedSource.ML_COPY.ToString());
                            }

                            if (UploadDescriptiveData != null)
                            {
                                try
                                {
                                    TrackChangeLog trackChangeLog = JsonConvert.DeserializeObject<TrackChangeLog>(UploadDescriptiveData.Value.ToString());

                                    if (trackChangeLog != null)
                                    {
                                        if (trackChangeLog.RefId != null)
                                            uploadId = (Guid)trackChangeLog.RefId;

                                        changeLog.Add(trackChangeLog);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "SetLiveArchiveAlbum > UploadDescriptiveData > DH Album Id : " + item.album_id);

                                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                                    {
                                        id = (int)_enServiceType,
                                        status = enServiceStatus.fail.ToString(),
                                        serviceName = _enServiceType.ToString(),
                                        timestamp = DateTime.Now,
                                        refId = refId.ToString()
                                    });
                                }
                            }

                            if (CopyDescriptiveData != null)
                            {
                                try
                                {
                                    TrackChangeLog trackChangeLog = JsonConvert.DeserializeObject<TrackChangeLog>(CopyDescriptiveData.Value.ToString());

                                    if (trackChangeLog != null)
                                    {
                                        if (trackChangeLog.RefId != null)
                                            uploadId = (Guid)trackChangeLog.RefId;

                                        changeLog.Add(trackChangeLog);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "SetLiveArchiveAlbum > CopyDescriptiveData > DH Album Id : " + item.album_id);

                                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                                    {
                                        id = (int)_enServiceType,
                                        status = enServiceStatus.fail.ToString(),
                                        serviceName = _enServiceType.ToString(),
                                        timestamp = DateTime.Now,
                                        refId = refId.ToString()
                                    });
                                }
                            }


                            albumOrgs.Add(new album_org()
                            {
                                id = uploadId,
                                original_album_id = item.album_id,
                                archive = false,
                                created_by = userId,
                                last_edited_by = userId,
                                ml_status = mlStatus,
                                org_id = orgId,
                                restricted = restricted,
                                org_workspace_id = orgWorkspaceId,
                                api_result_id = item.api_result_id,
                                source_deleted = item.archived,
                                chart_artist = isChartArtist,
                                change_log = changeLog == null ? null : JsonConvert.SerializeObject(changeLog, new JsonSerializerSettings())
                            });

                            if (item.api_result_id != 0)
                                lastSyncApiResultId = item.api_result_id;
                        }
                        if (albumOrgs.Count() > 0)
                            successCount += await _unitOfWork.TrackOrg.InsertUpdateAlbumOrg(albumOrgs);

                        _logger.LogDebug(mlMasterAlbums.Count() + " - Albums are succesfully synced");

                        //--- Check whether ml status has been changed
                        if (type == enWorkspaceLib.lib)
                        {
                            library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(refId, orgId);
                            newStatus = library_Org?.ml_status;
                        }
                        else
                        {
                            workspace_org workspace_Org = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(refId, orgId);
                            newStatus = workspace_Org?.ml_status;
                        }

                        //lastSyncApiResultId = (long)mlMasterAlbums.LastOrDefault()?.api_result_id;

                        if (type == enWorkspaceLib.ws)
                        {
                            await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                            {
                                last_album_sync_api_result_id = newStatus == mlStatus ? lastSyncApiResultId : 0,
                                workspace_id = refId,
                                org_id = orgId
                            });
                        }
                        else
                        {
                            await _unitOfWork.Library.UpdateLibraryOrg(new library_org()
                            {
                                last_album_sync_api_result_id = newStatus == mlStatus ? lastSyncApiResultId : 0,
                                library_id = refId,
                                org_id = orgId
                            });
                        }

                        if (newStatus == mlStatus)
                        {
                            mlMasterAlbums = await _unitOfWork.MLMasterTrack.GetMasterAlbumListForSetLive(type, refId, lastSyncApiResultId, library_Orgs, limit);

                            if (mlMasterAlbums.Count() == 0)
                            {

                            }
                        }
                        else
                        {
                            mlMasterAlbums = null;
                        }
                    }

                    if (type == enWorkspaceLib.ws)
                    {
                        await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                        {
                            org_workspace_id = wsLibOrgId,
                            album_sync_status = trackCount == successCount ? (int)enSyncStatus.SyncSuccess : (int)enSyncStatus.SyncFailed
                        });
                    }
                    else
                    {
                        await _unitOfWork.Library.UpdateLibraryOrg(new library_org()
                        {
                            org_library_id = wsLibOrgId,
                            album_sync_status = trackCount == successCount ? (int)enSyncStatus.SyncSuccess : (int)enSyncStatus.SyncFailed
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)_enServiceType,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = _enServiceType.ToString(),
                    timestamp = DateTime.Now,
                    refId = refId.ToString()
                });
                _logger.LogError(ex,"SetLiveArchiveAlbum");
                throw;
            }
        }
        public static List<string> GetNameListByRole(string role, ICollection<Soundmouse.Messaging.Model.InterestedParty> interestedParties)
        {
            if (interestedParties.Count > 0)
            {
                List<string> mLInterestedParties = interestedParties.Where(a => a.Role == role).Select(a => a.FullName.ReplaceSpecialCodes()).ToList();

                if (mLInterestedParties.Count > 0)
                    return mLInterestedParties;
            }
            return null;
        }

    }
}
