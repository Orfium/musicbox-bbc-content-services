using Microsoft.Extensions.Logging;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class LibraryWorkspaceActionLogic : ILibraryWorkspaceActionLogic
    {
        private readonly ILogger<DHTrackSync> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public LibraryWorkspaceActionLogic(ILogger<DHTrackSync> logger,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task SetAvailable(SyncActionPayload syncActionPayload)
        {
            enMLStatus _enMLStatus = syncActionPayload.action == enWorkspaceAction.SET_AVL_LOCKED.ToString() ? enMLStatus.Available_Loked : enMLStatus.Available;

            foreach (var item in syncActionPayload.ids)
            {
                if (syncActionPayload.type == "ws")
                {

                    workspace_org workspace_Org = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(Guid.Parse(item), syncActionPayload.orgid);

                    if (workspace_Org == null)
                    {
                        workspace_Org = await _unitOfWork.Workspace.SaveWorkspaceOrg(new workspace_org()
                        {
                            archived = false,
                            created_by = int.Parse(syncActionPayload.userId),
                            last_edited_by = int.Parse(syncActionPayload.userId),
                            ml_status = (int)_enMLStatus,
                            org_id = syncActionPayload.orgid,
                            org_workspace_id = Guid.NewGuid(),
                            restricted = false,
                            sync_status = (int)enSyncStatus.ToBeSynced,
                            workspace_id = Guid.Parse(item),
                            index_status = (int)enIndexStatus.ToBeIndexed,
                            album_sync_status = (int)enSyncStatus.ToBeSynced,
                            last_sync_api_result_id = 0,
                            last_album_sync_api_result_id = 0
                        });



                        await _unitOfWork.Workspace.UpdateWorkspace(new workspace()
                        {
                            workspace_id = Guid.Parse(item),
                            dh_status = (int)enDHStatus.Available
                        });

                        IEnumerable<library> libraries = await _unitOfWork.Library.GetLibraryListByWorkspaceId(Guid.Parse(item));

                        foreach (var lib in libraries)
                        {
                            await CheckAndInsertLibraryOrg(lib.library_id, lib.workspace_id, syncActionPayload.orgid, int.Parse(syncActionPayload.userId), _enMLStatus);
                        }

                        log_user_action actionLog = new log_user_action
                        {
                            data_type = enWorkspaceLib.ws.ToString(),
                            date_created = DateTime.Now,
                            user_id = Convert.ToInt32(syncActionPayload.userId),
                            org_id = syncActionPayload.orgid,
                            data_value = "",
                            action_id = (int)(enActionType)Enum.Parse(typeof(enActionType), syncActionPayload.action),
                            ref_id = workspace_Org.workspace_id,
                            status = (int)enLogStatus.Success
                        };
                        await _unitOfWork.ActionLogger.Log(actionLog);

                    }
                }
                else
                {
                    library library = await _unitOfWork.Library.GetById(Guid.Parse(item));
                    await CheckAndInsertLibraryOrg(Guid.Parse(item), library?.workspace_id, syncActionPayload.orgid, int.Parse(syncActionPayload.userId), _enMLStatus);
                    log_user_action actionLog = new log_user_action
                    {
                        data_type = enWorkspaceLib.lib.ToString(),
                        date_created = DateTime.Now,
                        user_id = Convert.ToInt32(syncActionPayload.userId),
                        org_id = syncActionPayload.orgid,
                        data_value = library.library_name,
                        action_id = (int)(enActionType)Enum.Parse(typeof(enActionType), syncActionPayload.action),
                        ref_id = library.library_id,
                        status = (int)enLogStatus.Success
                    };
                    await _unitOfWork.ActionLogger.Log(actionLog);
                }
            }
        }

        public async Task SetLive(SyncActionPayload syncActionPayload)
        {
            foreach (var item in syncActionPayload.ids)
            {
                if (syncActionPayload.type == enWorkspaceLib.ws.ToString())
                {

                    workspace_org workspace_Org = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(Guid.Parse(item), syncActionPayload.orgid);

                    if (workspace_Org != null)
                    {
                        workspace_Org.ml_status = syncActionPayload.action == "TAKEDOWN" ? (int)enMLStatus.Archive : (int)enMLStatus.Live;
                        workspace_Org.last_edited_by = int.Parse(syncActionPayload.userId);
                        workspace_Org.last_sync_api_result_id = 0;
                        workspace_Org.last_album_sync_api_result_id = 0;

                        if (await _unitOfWork.Workspace.UpdateWorkspaceOrg(workspace_Org) > 0)
                        {
                            await _unitOfWork.Workspace.UpdateWorkspace(new workspace()
                            {
                                workspace_id = Guid.Parse(item),
                                dh_status = (int)enDHStatus.Live
                            });

                            IEnumerable<library_org> libraries = await _unitOfWork.Library.GetOrgLibraryListByWorkspaceId(Guid.Parse(item), syncActionPayload.orgid);

                            foreach (var lib in libraries)
                            {
                                lib.ml_status = syncActionPayload.action == "TAKEDOWN" ? (int)enMLStatus.Archive : (int)enMLStatus.Live;
                                lib.last_edited_by = int.Parse(syncActionPayload.userId);

                                await _unitOfWork.Library.UpdateLibraryOrg(lib);
                            }

                            log_user_action actionLog = new log_user_action
                            {
                                data_type = enWorkspaceLib.lib.ToString(),
                                date_created = DateTime.Now,
                                user_id = Convert.ToInt32(syncActionPayload.userId),
                                org_id = syncActionPayload.orgid,
                                data_value = "",
                                action_id = (int)(enActionType)Enum.Parse(typeof(enActionType), syncActionPayload.action),
                                ref_id = workspace_Org.workspace_id,
                                status = (int)enLogStatus.Success
                            };
                            await _unitOfWork.ActionLogger.Log(actionLog);
                        }
                    }
                }
                else
                {
                    library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(Guid.Parse(item), syncActionPayload.orgid);
                    if (library_Org != null)
                    {
                        library_Org.ml_status = syncActionPayload.action == "TAKEDOWN" ? (int)enMLStatus.Archive : (int)enMLStatus.Live;
                        library_Org.last_edited_by = int.Parse(syncActionPayload.userId);
                        library_Org.last_sync_api_result_id = 0;
                        library_Org.last_album_sync_api_result_id = 0;

                        await _unitOfWork.Library.UpdateLibraryOrg(library_Org);
                        log_user_action actionLog = new log_user_action
                        {
                            data_type = enWorkspaceLib.lib.ToString(),
                            date_created = DateTime.Now,
                            user_id = Convert.ToInt32(syncActionPayload.userId),
                            org_id = syncActionPayload.orgid,
                            data_value = "",
                            action_id = (int)(enActionType)Enum.Parse(typeof(enActionType), syncActionPayload.action),
                            ref_id = library_Org.library_id,
                            status = (int)enLogStatus.Success
                        };
                        await _unitOfWork.ActionLogger.Log(actionLog);
                    }
                }
            }
        }

        public async Task<library_org> CheckAndInsertLibraryOrg(Guid libraryId, Guid? workspaceId, string orgId, int userId, enMLStatus _enMLStatus)
        {
            library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(libraryId, orgId);
            if (library_Org == null)
            {
                _logger.LogInformation("SyncNewLibrariesAfterSetLive Library id:{libraryId}, Status:{status} | Module:{module}", libraryId, _enMLStatus.ToString(), "Workspace Lib Sync");

                return await _unitOfWork.Library.SaveLibraryOrg(new library_org()
                {
                    archived = false,
                    created_by = userId,
                    last_edited_by = userId,
                    library_id = libraryId,
                    ml_status = (int)_enMLStatus,
                    org_id = orgId,
                    org_library_id = Guid.NewGuid(),
                    restricted = false,
                    sync_status = (int)enSyncStatus.ToBeSynced,
                    workspace_id = workspaceId,
                    album_sync_status = (int)enSyncStatus.ToBeSynced,
                    last_sync_api_result_id = 0,
                    last_album_sync_api_result_id = 0
                });
            }
            else if (_enMLStatus == enMLStatus.Available && library_Org.ml_status == (int)enMLStatus.Available_Loked)
            {
                library_Org.ml_status = (int)enMLStatus.Available;
                await _unitOfWork.Library.UpdateLibraryOrg(new library_org() { org_library_id = library_Org.org_library_id, ml_status = (int)enMLStatus.Available });
            }
            return library_Org;
        }

        public async Task PauseAction(SyncActionPayload pauseActionPayload)
        {
            var response = await _unitOfWork.Workspace.WorkspacePause(pauseActionPayload);
            if (response > 0)
            {
                await _unitOfWork.Workspace.UpdateDownloadStatus(pauseActionPayload, enLibWSDownloadStatus.Pause);
            }
            log_user_action actionLog = new log_user_action
            {
                data_type = enWorkspaceLib.ws.ToString(),
                date_created = DateTime.Now,
                user_id = Convert.ToInt32(pauseActionPayload.userId),
                org_id = pauseActionPayload.orgid,
                data_value = "",
                action_id = (int)(enActionType)Enum.Parse(typeof(enActionType), pauseActionPayload.action),
                ref_id = Guid.Parse(pauseActionPayload.ids[0]),
                status = (int)enLogStatus.Success
            };
            await _unitOfWork.ActionLogger.Log(actionLog);
        }
        public async Task ContinueAction(SyncActionPayload pauseActionPayload)
        {
            var status = await _unitOfWork.Workspace.GetPreviosStatusFromPause(Guid.Parse(pauseActionPayload.ids[0]));
            var result = await _unitOfWork.Workspace.UpdateDownloadStatus(pauseActionPayload, (enLibWSDownloadStatus)status);
            if (result > 0)
            {
                await _unitOfWork.Workspace.WorkspaceContinue(pauseActionPayload);
            }
            log_user_action actionLog = new log_user_action
            {
                data_type = enWorkspaceLib.ws.ToString(),
                date_created = DateTime.Now,
                user_id = Convert.ToInt32(pauseActionPayload.userId),
                org_id = pauseActionPayload.orgid,
                data_value = "",
                action_id = (int)(enActionType)Enum.Parse(typeof(enActionType), pauseActionPayload.action),
                ref_id = Guid.Parse(pauseActionPayload.ids[0]),
                status = (int)enLogStatus.Success
            };
            await _unitOfWork.ActionLogger.Log(actionLog);
        }

        public async Task GetWSByStatus(SyncActionPayload pauseActionPayload)
        {
            List<int> wsTypes = new List<int>();
        }

        public async Task SetArchive(SyncActionPayload syncActionPayload)
        {
            foreach (var item in syncActionPayload.ids)
            {
                if (syncActionPayload.type == enWorkspaceLib.ws.ToString())
                {

                    workspace_org workspace_Org = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(Guid.Parse(item), syncActionPayload.orgid);

                    if (workspace_Org != null)
                    {
                        workspace_Org.ml_status = (int)enMLStatus.Archive;
                        workspace_Org.last_edited_by = int.Parse(syncActionPayload.userId);
                        workspace_Org.last_sync_api_result_id = 0;
                        workspace_Org.last_album_sync_api_result_id = 0;

                        if (await _unitOfWork.Workspace.UpdateWorkspaceOrg(workspace_Org) > 0)
                        {
                            IEnumerable<library_org> libraries = await _unitOfWork.Library.GetOrgLibraryListByWorkspaceId(Guid.Parse(item), syncActionPayload.orgid);

                            foreach (var lib in libraries)
                            {
                                lib.ml_status = (int)enMLStatus.Live;
                                lib.last_edited_by = int.Parse(syncActionPayload.userId);

                                await _unitOfWork.Library.UpdateLibraryOrg(lib);
                            }
                        }
                    }
                }
                else
                {
                    library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(Guid.Parse(item), syncActionPayload.orgid);
                    if (library_Org != null)
                    {
                        library_Org.ml_status = (int)enMLStatus.Live;
                        library_Org.last_edited_by = int.Parse(syncActionPayload.userId);
                        library_Org.last_sync_api_result_id = 0;
                        library_Org.last_album_sync_api_result_id = 0;

                        await _unitOfWork.Library.UpdateLibraryOrg(library_Org);
                    }
                }
            }
        }

        public async Task Resync(SyncActionPayload syncActionPayload)
        {
            IEnumerable<workspace_org> workspace_orgs = await _unitOfWork.Workspace.GetWorkspaceOrgsByWorkspaceId(Guid.Parse(syncActionPayload.ids[0]));

            foreach (var workspaceOrg in workspace_orgs)
            {
                await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                {
                    sync_status=1,
                    index_status=1,
                    org_workspace_id = workspaceOrg.org_workspace_id,
                    last_sync_api_result_id = 0,
                    last_album_sync_api_result_id = 0,
                    last_edited_by = int.Parse(syncActionPayload.userId)
                });
            }            
        }
        public async Task UpdateMusicOrigin(SyncActionPayload syncActionPayload)
        {
            foreach (var id in syncActionPayload.ids)
            {
                await _unitOfWork.Workspace.UpdateMusicOrigin(new workspace_org()
                {
                    org_workspace_id = Guid.Parse(id),
                    music_origin = int.Parse(syncActionPayload.music_origin.music_origin)
                });

                await _unitOfWork.Workspace.UpdateWorkspaceOrg(new workspace_org()
                {
                    org_workspace_id = Guid.Parse(id),
                    last_sync_api_result_id = 0,
                    last_album_sync_api_result_id = 0,
                    last_edited_by = int.Parse(syncActionPayload.userId)
                });
            }
        }

        public async Task ChangeMLStatus(SyncActionPayload syncActionPayload)
        {
            enMLStatus mLStatus = enMLStatus.Live;

            switch ((enWorkspaceAction)Enum.Parse(typeof(enWorkspaceAction), syncActionPayload.action))
            {
                case enWorkspaceAction.SET_LIVE:
                case enWorkspaceAction.SET_ALIVE:
                    mLStatus = enMLStatus.Live;
                    break;

                case enWorkspaceAction.TAKEDOWN:
                    mLStatus = enMLStatus.Archive;
                    break;

                case enWorkspaceAction.RESTRICT:
                    mLStatus = enMLStatus.Restrict;
                    break;

                case enWorkspaceAction.UNDO_RESTRICT:
                    mLStatus = enMLStatus.Live;
                    break;
                case enWorkspaceAction.UNDO_TAKEDOWN:
                    mLStatus = enMLStatus.Live;
                    break;

                default:
                    break;
            }

            foreach (var item in syncActionPayload.ids)
            {
                if (syncActionPayload.type == enWorkspaceLib.ws.ToString())
                {

                    workspace_org workspace_Org = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(Guid.Parse(item), syncActionPayload.orgid);

                    if (workspace_Org != null && workspace_Org.ml_status != (int)mLStatus &&
                       !(mLStatus == enMLStatus.Live && workspace_Org.ml_status == (int)enMLStatus.Available_Loked))
                    {
                        workspace_Org.ml_status = (int)mLStatus;
                        workspace_Org.last_edited_by = int.Parse(syncActionPayload.userId);
                        workspace_Org.last_sync_api_result_id = 0;
                        workspace_Org.last_album_sync_api_result_id = 0;
                        workspace_Org.sync_status = (int)enSyncStatus.ToBeSynced;
                        workspace_Org.index_status = (int)enIndexStatus.ToBeIndexed;

                        if (await _unitOfWork.Workspace.UpdateWorkspaceOrg(workspace_Org) > 0)
                        {
                            await _unitOfWork.Workspace.UpdateWorkspace(new workspace()
                            {
                                workspace_id = Guid.Parse(item),
                                dh_status = (int)enDHStatus.Live
                            });

                            IEnumerable<library_org> libraries = await _unitOfWork.Library.GetOrgLibraryListByWorkspaceId(Guid.Parse(item), syncActionPayload.orgid);

                            foreach (var lib in libraries)
                            {
                                lib.ml_status = (int)mLStatus;
                                lib.last_edited_by = int.Parse(syncActionPayload.userId);

                                await _unitOfWork.Library.UpdateLibraryOrg(lib);
                            }
                        }
                    }
                }
                else
                {
                    library_org library_Org = await _unitOfWork.Library.GetLibraryByOrgId(Guid.Parse(item), syncActionPayload.orgid);
                    if (library_Org != null && library_Org.ml_status != (int)mLStatus)
                    {
                        library_Org.ml_status = (int)mLStatus;
                        library_Org.last_edited_by = int.Parse(syncActionPayload.userId);
                        library_Org.last_sync_api_result_id = 0;
                        library_Org.last_album_sync_api_result_id = 0;
                        library_Org.sync_status = (int)enSyncStatus.ToBeSynced;

                        await _unitOfWork.Library.UpdateLibraryOrg(library_Org);
                    }
                }
            }
        }
    }
}
