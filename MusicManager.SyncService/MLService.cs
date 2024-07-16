using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Helper;
using MusicManager.Logics.Logics;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elasticsearch.DataMatching;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using MusicManager.Infrastructure.Repository;

namespace MusicManager.SyncService
{
    public class MLService 
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IActionLoggerLogic _actionLoggerLogic;
        private readonly IDHTrackSync _dHTrackSync;
        private readonly ILogger<MLService> _logger;
        private readonly ILibraryWorkspaceActionLogic _libraryWorkspaceActionLogic;
        private readonly IAWSS3Repository _aWSS3Repository;
        private readonly IPlayoutLogic _playoutLogic;
        private readonly IElasticLogic _elasticLogic;
        private readonly ICtagLogic _ctagLogic;
        private readonly IWorkspaceService _workspaceService;
        private readonly IMLMasterTrackRepository _mLMasterTrackRepository;
        private int UserId = 59;
        private DateTime _takedownProcessDate = DateTime.Now.AddDays(-1);
        private DateTime _searchableByValidFromDate = DateTime.Now.AddDays(-1);
        private DateTime _preReleaseByValidFrom = DateTime.Now.AddDays(-1);
        private DateTime _chartTracksAndAlbumSyncDate = DateTime.Now.AddDays(-1);

        public MLService(IUnitOfWork unitOfWork,
            IOptions<AppSettings> appSettings,
             IActionLoggerLogic actionLoggerLogic,
             IDHTrackSync dHTrackSync,
             ILogger<MLService> logger,
             ILibraryWorkspaceActionLogic libraryWorkspaceActionLogic,
             IAWSS3Repository AWSS3Repository,
             IPlayoutLogic playoutLogic,
             IElasticLogic elasticLogic,
             ICtagLogic ctagLogic,
             IWorkspaceService workspaceService,
             IMLMasterTrackRepository mLMasterTrackRepository
            )
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
            _actionLoggerLogic = actionLoggerLogic;
            _dHTrackSync = dHTrackSync;
            _logger = logger;
            _libraryWorkspaceActionLogic = libraryWorkspaceActionLogic;
            _aWSS3Repository = AWSS3Repository;
            _playoutLogic = playoutLogic;
            _elasticLogic = elasticLogic;
            _ctagLogic = ctagLogic;
            _workspaceService = workspaceService;
            _mLMasterTrackRepository = mLMasterTrackRepository;
        }

        public async Task DownloadMasterDHTracks()
        {           
            try
            {
                IEnumerable<workspace> workspaces = await _unitOfWork.Workspace.GetMasterWorkspaceForSyncAsync(Guid.Parse(_appSettings.Value.MasterWSId));
                if (workspaces?.Count() > 0)
                {
                    foreach (workspace workspace in workspaces)
                    {
                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)enServiceType.Sync_Master_Workspace,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = enServiceType.Sync_Master_Workspace.ToString(),
                            timestamp = DateTime.Now,
                            refId = workspace.workspace_id.ToString()
                        });

                        log_sync_time logSyncTime = new log_sync_time()
                        {
                            service_id = 0,
                            track_download_start_time = DateTime.Now,
                            workspace_id = workspace.workspace_id
                        };

                        logSyncTime.download_tracks_count = await _dHTrackSync.DownloadDHTracks(workspace, enServiceType.Sync_Master_Workspace);
                        logSyncTime.track_download_end_time = DateTime.Now;
                        logSyncTime.track_download_time = logSyncTime.track_download_end_time - logSyncTime.track_download_start_time;

                        logSyncTime.album_download_start_time = DateTime.Now;
                        logSyncTime.download_albums_count = await _dHTrackSync.DownloadDHAlbums(workspace, enServiceType.Sync_Master_Workspace);
                        logSyncTime.album_download_end_time = DateTime.Now;
                        logSyncTime.album_download_time = logSyncTime.album_download_end_time - logSyncTime.album_download_start_time;

                        IEnumerable<workspace_org> workspace_Orgs = await _unitOfWork.Workspace.GetWorkspaceOrgsByWorkspaceId(workspace.workspace_id);

                        foreach (var item in workspace_Orgs)
                        {
                            var isPaused = await _unitOfWork.Workspace.CheckPause(item.workspace_id);
                            if (!isPaused)
                            {
                                logSyncTime.sync_start_time = DateTime.Now;
                                await _dHTrackSync.SyncTracks(item, enServiceType.Sync_Master_Workspace);
                                logSyncTime.sync_end_time = DateTime.Now;
                                logSyncTime.sync_time = logSyncTime.sync_end_time - logSyncTime.sync_start_time;

                                logSyncTime.track_index_start_time = DateTime.Now;
                                logSyncTime.index_tracks_count = await _dHTrackSync.ElasticIndex(item, enServiceType.Sync_Master_Workspace);
                                logSyncTime.track_index_end_time = DateTime.Now;
                                logSyncTime.track_index_time = logSyncTime.track_index_end_time - logSyncTime.track_index_start_time;

                                if (logSyncTime.index_tracks_count > 0)
                                    await Task.Delay(2000);

                                logSyncTime.album_index_start_time = DateTime.Now;
                                logSyncTime.index_albums_count = await _dHTrackSync.AlbumElasticIndex(item, enServiceType.Sync_Master_Workspace);
                                logSyncTime.album_index_end_time = DateTime.Now;
                                logSyncTime.album_index_time = logSyncTime.album_index_end_time - logSyncTime.album_index_start_time;
                            }
                        }

                        if (logSyncTime.download_tracks_count > 0 ||
                           logSyncTime.download_albums_count > 0 ||
                           logSyncTime.index_albums_count > 0 ||
                           logSyncTime.index_tracks_count > 0
                           )
                        {                          
                            logSyncTime.total_time = logSyncTime.track_download_time + logSyncTime.album_download_time + logSyncTime.sync_time + logSyncTime.track_index_time + logSyncTime.album_index_time;
                            logSyncTime.id = await _unitOfWork.logSyncTime.Save(logSyncTime);
                            await _unitOfWork.logSyncTime.UpdateLogSyncTime(logSyncTime);
                                                   
                            _logger.LogInformation(enServiceType.Sync_Master_Workspace.ToString() + " ({wsId}) - in {time} time | Download : {download_tracks_count}, Indexed : {index_tracks_count}", 
                                workspace.workspace_id, logSyncTime.total_time, logSyncTime.download_tracks_count, logSyncTime.index_tracks_count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Sync_Master_Workspace,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.Sync_Master_Workspace.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "DownloadMasterDHTracks");
            }
        }

        public async Task SyncWS()
        {
            try
            {
                _logger.LogDebug("---------------------------------------------------------------");
                _logger.LogDebug("Retrieving Workspace list from Datahub - " + DateTime.Now);
                
                List<MetadataWorkspace> MetadataWorkspaces = await _unitOfWork.MetadataAPI.GetAllWorkspaces();

                if (MetadataWorkspaces != null)
                {
                    _logger.LogDebug("Source Workspace count : " + MetadataWorkspaces.Count);

                    int lineNo = 1;

                    List<staging_workspace> stagingWorkspace = new List<staging_workspace>();

                    foreach (MetadataWorkspace item in MetadataWorkspaces)
                    {
                        stagingWorkspace.Add(new staging_workspace()
                        {
                            workspace_name = item.name,
                            workspace_id = new Guid(item.id),
                            track_count = item.trackCount,
                            deleted = item.deleted
                        });

                        if (lineNo % 1000 == 0)
                        {
                            await _unitOfWork.WorkspaceStaging.BulkInsert(stagingWorkspace);
                            stagingWorkspace = new List<staging_workspace>();
                        }

                        lineNo++;
                    }

                    await _unitOfWork.WorkspaceStaging.BulkInsert(stagingWorkspace);

                    _unitOfWork.Workspace.SyncWorkspaces(UserId);

                    _logger.LogDebug("Workspace sync completed - " + DateTime.Now);
                    _logger.LogDebug("---------------------------------------------------------------");

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Workspace_Library_Sync_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Workspace_Library_Sync_Service.ToString(),
                        timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncWS");               
            }  
        }

        public async Task SyncNewLibrariesAfterSetLive()
        {
            IEnumerable<library> workspaces = await _unitOfWork.Library.GetNewDistinctWorkspacesAfterLive();

            foreach (library item in workspaces)
            {
                IEnumerable<workspace_org> workspaceOrgs = await _unitOfWork.Workspace.GetWorkspaceOrgsByWorkspaceId(item.workspace_id);

                foreach (workspace_org workspaceOrg in workspaceOrgs)
                {
                    _logger.LogInformation("SyncNewLibrariesAfterSetLive workspace id:{workspaceId} | Module:{module}", item.workspace_id, "Workspace Lib Sync");

                    IEnumerable<library> libraries = await _unitOfWork.Library.GetLibraryListByWorkspaceId(item.workspace_id);

                    foreach (var lib in libraries)
                    {
                        await _libraryWorkspaceActionLogic.CheckAndInsertLibraryOrg(lib.library_id, lib.workspace_id, workspaceOrg.org_id, workspaceOrg.created_by, (enMLStatus)workspaceOrg.ml_status);
                    }
                }

                //--- Resync workspace 
                SyncActionPayload syncActionPayload = new SyncActionPayload()
                {
                    ids = new List<string>()
                };
                syncActionPayload.userId = "0";
                syncActionPayload.ids.Add(item.workspace_id.ToString());
                await _libraryWorkspaceActionLogic.Resync(syncActionPayload);
            }
        }

        public async Task SyncLibrary()
        {
            try
            {
                _logger.LogDebug("---------------------------------------------------------------");
                _logger.LogDebug("Retrieving Library list from Datahub - " + DateTime.Now);
                
                List<MetadataLibrary> MetadataLibraries = await _unitOfWork.MetadataAPI.GetAllLibraries();

                if (MetadataLibraries != null)
                {
                    _logger.LogDebug("Source Library count : " + MetadataLibraries.Count);

                    int lineNo = 1;

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Workspace_Library_Sync_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Workspace_Library_Sync_Service.ToString(),
                        timestamp = DateTime.Now
                    });

                    List<staging_library> stagingLibrary = new List<staging_library>();

                    foreach (MetadataLibrary item in MetadataLibraries)
                    {
                        stagingLibrary.Add(new staging_library()
                        {
                            library_name = item.name,
                            library_id = new Guid(item.id),
                            workspace_id = new Guid(item.workspaceid),
                            track_count = item.trackCount,
                            deleted = item.deleted,
                            date_created = CommonHelper.GetCurrentUtcEpochTime()
                        });

                        if (lineNo % 1000 == 0)
                        {
                            await _unitOfWork.LibraryStaging.BulkInsert(stagingLibrary);
                            stagingLibrary = new List<staging_library>();
                        }
                        lineNo++;
                    }

                    await _unitOfWork.LibraryStaging.BulkInsert(stagingLibrary);

                    _unitOfWork.Library.SyncLibraries(UserId);

                    _logger.LogDebug("Library sync completed - " + DateTime.Now);
                    _logger.LogDebug("---------------------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncLibrary");
            }
        }

        public async Task DownloadDHTracks()
        {
            try
            {
                IEnumerable<workspace> workspaces = await _unitOfWork.Workspace.GetWorkspacesForSyncAsync();

                if (workspaces?.Count() > 0)
                {
                    foreach (workspace ws in workspaces)
                    {
                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)enServiceType.Sync_External_Workspace,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = enServiceType.Sync_External_Workspace.ToString(),
                            timestamp = DateTime.Now,
                            refId = ws.workspace_id.ToString()
                        });

                        log_sync_time logSyncTime = new log_sync_time();

                        if (ws.workspace_id != Guid.Parse(_appSettings.Value.MasterWSId) && await _unitOfWork.Workspace.CheckPause(ws.workspace_id) == false)
                        {
                            logSyncTime = new log_sync_time()
                            {
                                service_id = _appSettings.Value.SyncServiceId,
                                track_download_start_time = DateTime.Now,
                                workspace_id = ws.workspace_id
                            };

                            logSyncTime.download_tracks_count = await _dHTrackSync.DownloadDHTracks(ws, enServiceType.Sync_External_Workspace);
                            logSyncTime.track_download_end_time = DateTime.Now;
                            logSyncTime.track_download_time = logSyncTime.track_download_end_time - logSyncTime.track_download_start_time;


                            logSyncTime.album_download_start_time = DateTime.Now;
                            logSyncTime.download_albums_count = await _dHTrackSync.DownloadDHAlbums(ws, enServiceType.Sync_External_Workspace);
                            logSyncTime.album_download_end_time = DateTime.Now;
                            logSyncTime.album_download_time = logSyncTime.album_download_end_time - logSyncTime.album_download_start_time;


                            IEnumerable<workspace_org> workspace_Orgs = await _unitOfWork.Workspace.GetWorkspaceOrgsByWorkspaceId(ws.workspace_id);
                            foreach (var wsOrg in workspace_Orgs)
                            {
                                logSyncTime.org_id = wsOrg.org_id;

                                var isPaused = await _unitOfWork.Workspace.CheckPause(wsOrg.workspace_id);
                                if (!isPaused)
                                {
                                    logSyncTime.sync_start_time = DateTime.Now;

                                    await _dHTrackSync.SyncTracks(wsOrg, enServiceType.Sync_External_Workspace);

                                    logSyncTime.sync_end_time = DateTime.Now;

                                    logSyncTime.sync_time = logSyncTime.sync_end_time - logSyncTime.sync_start_time;

                                    logSyncTime.track_index_start_time = DateTime.Now;
                                    logSyncTime.index_tracks_count = await _dHTrackSync.ElasticIndex(wsOrg, enServiceType.Sync_External_Workspace);
                                    logSyncTime.track_index_end_time = DateTime.Now;
                                    logSyncTime.track_index_time = logSyncTime.track_index_end_time - logSyncTime.track_index_start_time;

                                    if (logSyncTime.index_tracks_count > 0)
                                        await Task.Delay(10000);

                                    logSyncTime.album_index_start_time = DateTime.Now;
                                    logSyncTime.index_albums_count = await _dHTrackSync.AlbumElasticIndex(wsOrg, enServiceType.Sync_External_Workspace);
                                    logSyncTime.album_index_end_time = DateTime.Now;
                                    logSyncTime.album_index_time = logSyncTime.album_index_end_time - logSyncTime.album_index_start_time;

                                }
                            }
                        }

                        if (logSyncTime.download_tracks_count > 0 ||
                           logSyncTime.index_albums_count > 0 ||
                           logSyncTime.index_tracks_count > 0
                           )
                        {
                            logSyncTime.total_time = logSyncTime.track_download_time + logSyncTime.album_download_time + logSyncTime.sync_time + logSyncTime.track_index_time + logSyncTime.album_index_time;
                            logSyncTime.id = await _unitOfWork.logSyncTime.Save(logSyncTime);
                            await _unitOfWork.logSyncTime.UpdateLogSyncTime(logSyncTime);

                            _logger.LogInformation(enServiceType.Sync_External_Workspace.ToString() + " ({wsId}) - in {time} time | Download : {download_tracks_count}, Indexed : {index_tracks_count}",
                              ws.workspace_id, logSyncTime.total_time, logSyncTime.download_tracks_count, logSyncTime.index_tracks_count);
                        }
                    }
                    _logger.LogDebug("Track Sync Completed Success");
                }
                else
                {
                    _logger.LogDebug("No workspace found");
                }
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Sync_External_Workspace,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.Sync_External_Workspace.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "Sync External Workspace");
            }
        }

        public async Task ProcessUploadedTracks()
        {
            try
            {
                List<upload_track> upload_Tracks = await _unitOfWork.UploadTrack.GetTracksForAssetUpload();

                if (upload_Tracks?.Count() > 0)
                {
                    List<MA_BulkUploadPayload> bulkUploadPayloads = new List<MA_BulkUploadPayload>();

                    _logger.LogDebug("Found - " + upload_Tracks.Count() + " tracks");

                    org_user org_User = null;

                    List<c_tag> c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;


                    int x = 0;

                    for (int i = 0; i < upload_Tracks.Count(); i++)
                    {
                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)enServiceType.Track_Upload_Service,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = enServiceType.Track_Upload_Service.ToString(),
                            timestamp = DateTime.Now
                        });

                        org_User = await _unitOfWork.User.GetUserById((int)upload_Tracks[i].created_by);

                        if (org_User == null)
                            org_User = new org_user() { user_id = (int)upload_Tracks[i].created_by };

                        //string dhTrackId = null;
                        //string dhAlbumId = null;

                        #region -----------  Create Album ------------------------------------------------------------------------------------

                        //upload_album upload_Album = await _unitOfWork.UploadAlbum.FirstOrDefualt(a => a.id == upload_Tracks[i].ml_album_id);

                        //if (upload_Album != null && upload_Album?.dh_album_id == null)
                        //{
                        //    DHAlbum dHAlbum = await _unitOfWork.MusicAPI.PrepareAndCreateAlbum(_appSettings.Value.MasterWSId, upload_Album, org_User);

                        //    if (dHAlbum!=null)
                        //    {
                        //        upload_Tracks[i].dh_album_id = dHAlbum.id;
                        //        upload_Album.dh_album_id = dHAlbum.id;
                        //        upload_Album.modified = false;
                        //        await _unitOfWork.UploadAlbum.UpdateAlbumByDHAlbumId(upload_Album);
                        //    }
                        //}

                        #endregion

                        #region -----------  Upload Artwork ------------------------------------------------------------------------------------
                        //try
                        //{
                        //    if (upload_Tracks[i].dh_album_id != null && upload_Tracks[i].artwork_uploaded == true && upload_Album.artwork_uploaded == null)
                        //    {
                        //        string _artWorkKey = $"{_appSettings.Value.AWSS3.FolderName}/ARTWORK/{upload_Album.session_id}_{upload_Album.id}.jpg";

                        //        byte[] _stream = await _aWSS3Repository.GetImageStreamById(_artWorkKey);

                        //        if (_stream != null)
                        //        {
                        //            HttpStatusCode httpStatusCode = await _unitOfWork.MusicAPI.UploadArtwork(upload_Album.dh_album_id.ToString(), _stream);
                        //            if (httpStatusCode == HttpStatusCode.Created)
                        //            {
                        //                upload_Album.artwork_uploaded = true;
                        //            }
                        //            else
                        //            {
                        //                upload_Album.artwork_uploaded = false;
                        //            }
                        //            await _unitOfWork.UploadAlbum.UpdateArtworkUploaded(upload_Album);
                        //        }
                        //    }
                        //}
                        //catch (Exception)
                        //{

                        //}
                        #endregion

                        #region -----------  Create Track ------------------------------------------------------------------------------------
                        //if (upload_Tracks[i].dh_track_id == null)
                        //{
                        //    DHTrack dHTrack = await _unitOfWork.MusicAPI.CreateUploadTrack(_appSettings.Value.MasterWSId, upload_Tracks[i], upload_Album?.dh_album_id, org_User);

                        //    if (dHTrack != null)
                        //    {
                        //        upload_Tracks[i].modified = false;
                        //        upload_Tracks[i].dh_track_id = dHTrack.id;

                        //        await _unitOfWork.UploadTrack.UpdateDHTrackId(upload_Tracks[i]);
                        //    }
                        //}
                        #endregion

                        #region -----------  Sync upload assets ------------------------------------------------------------------------------------

                        if (upload_Tracks[i].dh_track_id != null && upload_Tracks[i].asset_upload_status == "S3 Success")
                        {
                            DHTrack dHTrack = await _unitOfWork.MusicAPI.GetTrackById(upload_Tracks[i].dh_track_id.ToString());
                            if (dHTrack == null)
                            {
                                upload_Tracks[i].asset_upload_status = "error";
                                upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                upload_Tracks[i].asset_uploaded = false;
                                await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                            }
                            else if (dHTrack.audio?.size > 0)
                            {
                                upload_Tracks[i].asset_upload_status = "completed";
                                upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                upload_Tracks[i].asset_uploaded = true;
                                await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                            }
                            else
                            {
                                byte[] _stream = await _aWSS3Repository.GetImageStreamById(_appSettings.Value.AWSS3.FolderName + "/" + upload_Tracks[i].s3_id);

                                if (_stream != null)
                                {
                                    HttpStatusCode httpStatusCode = await _unitOfWork.MusicAPI.UploadTrack(upload_Tracks[i].dh_track_id.ToString(), _stream);
                                    if (httpStatusCode == HttpStatusCode.Created)
                                    {
                                        upload_Tracks[i].asset_upload_status = "completed";
                                        upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                        upload_Tracks[i].asset_uploaded = true;
                                        await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);

                                        //--- Update PRS
                                        //await _ctagLogic.UpdatePRSforTrack((Guid)upload_Tracks[i].upload_id, null, c_Tags);
                                    }
                                    else
                                    {
                                        upload_Tracks[i].asset_upload_status = "error";
                                        upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                        upload_Tracks[i].asset_uploaded = false;
                                        await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                                    }
                                }
                                else
                                {
                                    upload_Tracks[i].asset_upload_status = "error";
                                    upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                    upload_Tracks[i].asset_uploaded = false;
                                    await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                                }
                            }
                        }
                        #endregion

                        #region -----------  Add to bulkupload ------------------------------------------------------------------------------------

                        //if (dhTrackId != null && upload_Tracks[i].asset_upload_status == "S3 Success")
                        //{
                        //    bulkUploadPayloads.Add(new MA_BulkUploadPayload()
                        //    {
                        //        bucket = _appSettings.Value.AWSS3.BucketName,
                        //        key = _appSettings.Value.AWSS3.FolderName + "/" + upload_Tracks[i].s3_id,
                        //        region = _appSettings.Value.AWSS3.Reagion,
                        //        trackId = dhTrackId
                        //    });
                        //}
                        #endregion

                        _logger.LogDebug(string.Format("Checked and created {0} / {1}", x++, upload_Tracks.Count()));
                    }
                }

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Track_Upload_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Track_Upload_Service.ToString(),
                    timestamp = DateTime.Now
                });

            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Track_Upload_Service,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.Track_Upload_Service.ToString(),
                    timestamp = DateTime.Now
                });
                _logger.LogError(ex, "ProcessUploadedTracks");
            }

            #region -----------  Import Begin ------------------------------------------------------------------------------------
            //if (bulkUploadPayloads.Count() > 0)
            //{
            //    HttpWebResponse httpWebResponse = await _unitOfWork.MusicAPI.SendImportBegin(bulkUploadPayloads);

            //    if (httpWebResponse != null && httpWebResponse.StatusCode == HttpStatusCode.OK)
            //    {
            //        using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            //        {
            //            string result = streamReader.ReadToEnd();
            //            dynamic aa = JsonConvert.DeserializeObject<dynamic>(result);
            //        }

            //        for (int i = 0; i < upload_Tracks.Count(); i++)
            //        {
            //            if (upload_Tracks[i].dh_track_id != null && upload_Tracks[i].asset_upload_status == "S3 Success")
            //            {
            //                upload_Tracks[i].asset_upload_status = "Begin";
            //                upload_Tracks[i].asset_upload_begin = DateTime.Now;
            //                await _unitOfWork.UploadTrack.UpdateTrackByStatus(upload_Tracks[i]);
            //            }
            //        }


            //        _logger.LogDebug(string.Format("Upload begin done"));
            //    }
            //}
            #endregion                     
        }

        public async Task PublishPlayouts()
        {
            try
            {               
                await _playoutLogic.ProcessPublishPlayOut();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublishPlayouts | Module: {Module}", "Playout");
            }
        }

        public async Task PlayoutS3Cleanup()
        {
            try
            {                
                await _playoutLogic.S3Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PlayoutS3Cleanup | Module: {Module}", "Playout");
            }
        }

        public async Task SyncMasterCharts()
        {
            if (_chartTracksAndAlbumSyncDate.Date != DateTime.Now.Date)
            {

                string trackChartTypeId = "b3c7f8d9-f5b7-4856-a933-25539e63ee37";
                string albumChartTypeId = "a8fc9a18-e137-49dd-bdaf-9eae00f41d88";

                chart_sync_summary trackChartSyncSummary = new chart_sync_summary()
                {
                    chart_type_id = Guid.Parse(trackChartTypeId),
                    type = "t",
                    check_date = DateTime.Now.AddMonths(-1)
                };

                chart_sync_summary albumChartSyncSummary = new chart_sync_summary()
                {
                    chart_type_id = Guid.Parse(trackChartTypeId),
                    type = "a",
                    check_date = DateTime.Now.AddMonths(-1)
                };

                MasterTrackChartResponse masterTrackChartResponse = await _unitOfWork.Chart.GetAllTrackMasterTracks(trackChartSyncSummary, trackChartTypeId);
                _logger.LogDebug("Master track count - " + masterTrackChartResponse?.results.Count());
                if (masterTrackChartResponse?.results.Count() > 0)
                {
                    int count = await _unitOfWork.Chart.InsertUpdateMasterTracksBulk(masterTrackChartResponse.results);
                    _logger.LogInformation("Imported track count - " + count);
                }

                MasterAlbumChartResponse masterAlbumChartResponse = await _unitOfWork.Chart.GetAllMasterAlbums(albumChartSyncSummary, albumChartTypeId);
                _logger.LogDebug("Master album count - " + masterAlbumChartResponse?.results.Count());
                if (masterAlbumChartResponse?.results.Count() > 0)
                {
                    int count = await _unitOfWork.Chart.InsertUpdateMasterAlbumBulk(masterAlbumChartResponse.results);
                    _logger.LogInformation("Imported album count - " + count);
                }
                _chartTracksAndAlbumSyncDate = DateTime.Now;
            }
        }

        public async Task TakedownByValidTo()
        {
            int size = 200;
            long completedCount = 0;           

            if (_takedownProcessDate.Date < DateTime.Now.Date && _appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour)
            {
                //--- Trigger SyncMasterCharts service
                await SyncMasterCharts();

                DateTime startTime = DateTime.Now;
                long toBeTakedownCount = await _elasticLogic.GetTakedownTracksCount();
                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchTakedownTracks(size);

                _logger.LogInformation("TakedownByValidTo Count - {count}", toBeTakedownCount);

                if (toBeTakedownCount > 0)
                {
                    while (mLTrackDocuments.Count() > 0)
                    {
                        mLTrackDocuments.ToList().ForEach(c => { c.archived = true; c.sourceDeleted = true; c.takedownDate = DateTime.Now; c.takedownType = enTakedownType.EXP.ToString(); });

                        var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments.ToList());

                        if (asyncIndexResponse.Length > 0)
                        {
                            completedCount += mLTrackDocuments.Count() - asyncIndexResponse.Length;
                            _logger.LogError("TakedownByValidTo > " + asyncIndexResponse.ToString());
                        }
                        else
                        {
                            completedCount += mLTrackDocuments.Count();
                        }                       

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        mLTrackDocuments = await _elasticLogic.SearchTakedownTracks(size);

                        _logger.LogInformation("Completed (TakedownByValidTo) - " + completedCount + " / " + toBeTakedownCount);
                    }

                    var Summary = new
                    {
                        service_start_datetime = startTime,
                        service_end_datetime = DateTime.Now,
                        tracks_to_be_takendown = toBeTakedownCount,
                        completed_count = completedCount
                    };

                    _logger.LogInformation(enServiceType.Takedown_Service.ToString() + " Start - {@startTime} / End - {@endTime} - {@summary}", startTime, Summary.service_end_datetime, Summary);

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Takedown_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Takedown_Service.ToString(),
                        timestamp = DateTime.Now,
                        summary = Summary
                    });
                }

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Takedown_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Takedown_Service.ToString(),
                    timestamp = DateTime.Now
                });
                _takedownProcessDate = DateTime.Now;
            }
        }

        public async Task SearchableByValidFrom()
        {
            int size = 200;
            long completedCount = 0;            

            if (_searchableByValidFromDate.Date < DateTime.Now.Date && _appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour)
            {
                DateTime startTime = DateTime.Now;
                long preReleaseCount = await _elasticLogic.GetNotPreReleaseTracksCount();
                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchNotPreReleaseTracks(size);

                _logger.LogInformation("SearchableByValidFrom Count - {count}", preReleaseCount);

                if (preReleaseCount > 0)
                {
                    while (mLTrackDocuments.Count() > 0)
                    {
                        mLTrackDocuments.ToList().ForEach(c => { c.preRelease = false; c.searchableFrom = DateTime.Now; c.searchableType = enPreReleaseType.EXP.ToString(); });

                        var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments.ToList());

                        if (asyncIndexResponse.Length > 0)
                        {
                            completedCount += mLTrackDocuments.Count() - asyncIndexResponse.Length;
                            _logger.LogError("PreReleaseByValidFrom > " + asyncIndexResponse.ToString());
                        }
                        else
                        {
                            completedCount += mLTrackDocuments.Count();
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        mLTrackDocuments = await _elasticLogic.SearchNotPreReleaseTracks(size);

                        _logger.LogInformation("Completed (SearchableByValidFrom) - " + completedCount + " / " + preReleaseCount);
                    }

                    var Summary = new
                    {
                        service_start_datetime = startTime,
                        service_end_datetime = DateTime.Now,
                        searchable_count = preReleaseCount,
                        completed_count = completedCount
                    };

                    _logger.LogInformation(enServiceType.Set_Searchable_Service.ToString() + " Start - {@startTime} / End - {@endTime} - {@summary}", startTime, Summary.service_end_datetime, Summary);

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Set_Searchable_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Set_Searchable_Service.ToString(),
                        timestamp = DateTime.Now,
                        summary = Summary
                    });
                }
                _searchableByValidFromDate = DateTime.Now;

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Set_Searchable_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Set_Searchable_Service.ToString(),
                    timestamp = DateTime.Now
                });
            }
        }

        public async Task PreReleaseByValidFrom()
        {
            int size = 200;
            long completedCount = 0;            

            if (_preReleaseByValidFrom.Date < DateTime.Now.Date && _appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour)
            {
                DateTime startTime = DateTime.Now;
                long preReleaseCount = await _elasticLogic.GetPreReleaseTracksCount();
                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchPreReleaseTracks(size);

                _logger.LogInformation("PreReleaseByValidFrom Count - {count}", preReleaseCount);

                if (preReleaseCount > 0)
                {
                    while (mLTrackDocuments.Count() > 0)
                    {
                        mLTrackDocuments.ToList().ForEach(c => { c.preRelease = true; c.searchableFrom = null; c.searchableType = enPreReleaseType.DH.ToString(); });

                        var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments.ToList());

                        if (asyncIndexResponse.Length > 0)
                        {
                            completedCount += mLTrackDocuments.Count() - asyncIndexResponse.Length;
                            _logger.LogError("PreReleaseByValidFrom > " + asyncIndexResponse.ToString());
                        }
                        else
                        {
                            completedCount += mLTrackDocuments.Count();
                        }                        

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        mLTrackDocuments = await _elasticLogic.SearchPreReleaseTracks(size);

                        _logger.LogInformation("Completed (PreReleaseByValidFrom)- " + completedCount + " / " + preReleaseCount);
                    }

                    var Summary = new
                    {
                        service_start_datetime = startTime,
                        service_end_datetime = DateTime.Now,
                        pre_release_count = preReleaseCount,
                        completed_count = completedCount
                    };

                    _logger.LogInformation(enServiceType.Set_Pre_Release_Service.ToString() + " Start - {@startTime} / End - {@endTime} - {@summary}", startTime, Summary.service_end_datetime, Summary);

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Set_Pre_Release_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Set_Pre_Release_Service.ToString(),
                        timestamp = DateTime.Now,
                        summary = Summary
                    });
                }
                _preReleaseByValidFrom = DateTime.Now;

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Set_Pre_Release_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Set_Pre_Release_Service.ToString(),
                    timestamp = DateTime.Now
                });
            }
        }

        public async Task PRSIndex(bool charted)
        {
            int size = 300;
            int indexedCount = 0;
            int totalTrackCount = 0;
            int prsSessionError = 0;
            int prsFound = 0;
            int prsNotFound = 0;
            PRSUpdateReturn pRSUpdateReturn = null;
            List<c_tag> c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;
            List<MLTrackDocument> indexDocumnets = new List<MLTrackDocument>();

            try
            {
                _logger.LogInformation("PRSIndex service - Start - " + DateTime.Now);

                List<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchTracksForPRSIndex(size);

                if (mLTrackDocuments.Count() == 0)
                {
                    _logger.LogInformation("PRSIndex service - ascending - completed" + DateTime.Now);
                    await Task.Delay(TimeSpan.FromMinutes(10));
                }

                indexedCount = 0;
                prsFound = 0;
                prsNotFound = 0;

                totalTrackCount += mLTrackDocuments.Count();

                for (int j = 0; j < mLTrackDocuments.Count; j++)
                {
                    pRSUpdateReturn = await _ctagLogic.UpdatePRSforTrack(mLTrackDocuments[j].id, mLTrackDocuments[j], c_Tags, charted, false);

                    //--- If the PRS session id is not returned add 5min delay
                    if (pRSUpdateReturn?.prsSessionNotFound == true)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }

                    //--- If the PRS search is failed add 1 min delay
                    if (pRSUpdateReturn?.prsSearchError == true)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }

                    if (pRSUpdateReturn?.prsSearchError != true
                        && pRSUpdateReturn?.mLTrackDocument != null
                        && pRSUpdateReturn?.prsSessionNotFound != true)
                        indexDocumnets.Add(pRSUpdateReturn?.mLTrackDocument);

                    if (pRSUpdateReturn?.prsFound == true)
                    {
                        _logger.LogDebug("PRS found - " + mLTrackDocuments[j].id);
                        prsFound++;
                    }
                    else if (pRSUpdateReturn.prsSessionNotFound)
                    {
                        prsSessionError++;
                    }
                    else
                    {
                        prsNotFound++;
                    }
                    indexedCount++;
                }

                _logger.LogInformation("PRS Index summary - {indexedCount} (Found: {prsFound} / Not found: {prsNotFound} / Session Error: {prsSessionError}) | Total {@totalTrackCount}, Module:{Module}", indexedCount, prsFound, prsNotFound, prsSessionError, totalTrackCount, "PRS Service");


                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.PRS_Index_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.PRS_Index_Service.ToString(),
                    timestamp = DateTime.Now
                });

                if (indexDocumnets.Count() > 0)
                {
                    await _elasticLogic.BulkIndexTrackDocument(indexDocumnets);
                }
                _logger.LogDebug("PRSIndex service - End - " + DateTime.Now);

            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.PRS_Index_Service,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.PRS_Index_Service.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "PRSIndex");
            }
        }

        public async Task LiveTrackCopyToMLDatabase()
        {
            var trackDocs = await _elasticLogic.SearchLiveTracks();
            var workspaceOrgs = await _workspaceService.GetWorkspaceOrgsByWorkspaceId(Guid.Parse(_appSettings.Value.MasterWSId));
            workspace_org workspaceOrg = workspaceOrgs.First();

            foreach (var trackDoc in trackDocs)
            {
                if (trackDoc.id.ToString() == "962eafc9-2811-4add-aac9-72b69d8ee6ad") {
                    ml_master_track ml_Master_Track = await _mLMasterTrackRepository.GetMaterTrackById((Guid)trackDoc.dhTrackId);

                    if (ml_Master_Track == null)
                    {
                        Soundmouse.Messaging.Model.Track track = trackDoc.MLTrackDocumentToDHTrack();

                        Guid mlVersionId = Guid.NewGuid();

                        await _unitOfWork.TrackAPIResults.Insert(new log_track_api_results()
                        {
                            api_call_id = 0,
                            deleted = false,
                            metadata = JsonConvert.SerializeObject(track, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                            workspace_id = Guid.Parse(_appSettings.Value.MasterWSId),
                            track_id = track.Id,
                            received = 0,
                            version_id = mlVersionId,
                            session_id = 0,
                            created_by = Guid.NewGuid(),
                            date_created = DateTime.Now
                        });

                        track_org track_Org = new track_org()
                        {
                            id = trackDoc.id,
                            original_track_id = track.Id,
                            album_id = track?.TrackData?.Product?.Id ?? null,
                            archive = false,
                            created_by = 0,
                            last_edited_by = 0,
                            source_deleted = false,
                            ml_status = (int)enMLStatus.Live,
                            restricted = false,
                            api_result_id = 0,
                            org_id = "n2eu7wcxhyhmoj0fxgsf",
                            clearance_track = true,
                            org_workspace_id = workspaceOrg.org_workspace_id,
                        };

                        await _unitOfWork.TrackOrg.InsertUpdateTrackOrg(new List<track_org>() { track_Org });
                    }
                    else
                    {
                        Console.WriteLine($"Master track found - {trackDoc.dhTrackId}");
                    }
                }

               
            }
        }
    }
}
