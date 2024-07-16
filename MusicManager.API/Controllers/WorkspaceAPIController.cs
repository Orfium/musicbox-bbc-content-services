using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Infrastructure.Repository;
using MusicManager.Logics.ServiceLogics;

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceAPIController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrgExcludeLogic _orgExcludeLogic;
        private readonly ILibraryWorkspaceActionLogic _libraryWorkspaceActionLogic;
        private readonly IElasticLogic _elasticLogic;
        private readonly ILogger<WorkspaceRepository> _logger;

        public WorkspaceAPIController(IUnitOfWork unitOfWork, IOrgExcludeLogic orgExcludeLogic, 
            ILibraryWorkspaceActionLogic libraryWorkspaceActionLogic,
            IElasticLogic elasticLogic,
            ILogger<WorkspaceRepository> logger)
        {
            _unitOfWork = unitOfWork;
            _orgExcludeLogic = orgExcludeLogic;
            _libraryWorkspaceActionLogic = libraryWorkspaceActionLogic;
            _elasticLogic = elasticLogic;
            _logger = logger;
        }



        [HttpPost("Save")]
        public async Task<IActionResult> Register([FromBody] MusicManager.Core.Models.workspace workspace)
        {
            try
            {
                workspace.workspace_id = new Guid();
                await _unitOfWork.Workspace.Add(workspace);
                if (await _unitOfWork.Complete() == 1)
                {
                    log_user_action actionLog = new log_user_action
                    {
                        data_type = enWorkspaceLib.ws.ToString(),
                        date_created = DateTime.Now,
                        user_id = workspace.created_by,
                        org_id = "",
                        data_value = workspace.workspace_name,
                        action_id = (int)enActionType.SaveWorkspace,
                        ref_id = workspace.workspace_id, // ws id
                        status = 1
                    };
                    await _unitOfWork.ActionLogger.Log(actionLog);
                    return Ok(workspace);
                }

                return BadRequest();
            }
            catch (Exception ex)
            {               
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("WorkspaceAction")]
        public async Task<IActionResult> WorkspaceAction(SyncActionPayload syncActionPayload)
        {
            int result = 1;

            if (syncActionPayload?.ids.Count == 0)
                return BadRequest();

            switch ((enWorkspaceAction)Enum.Parse(typeof(enWorkspaceAction), syncActionPayload.action))
            {
                case enWorkspaceAction.SET_AVL:
                case enWorkspaceAction.SET_AVL_LOCKED:
                    _logger.LogInformation("Set Available Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    await _libraryWorkspaceActionLogic.SetAvailable(syncActionPayload);
                    break;
                case enWorkspaceAction.SET_ALIVE:
                    _logger.LogInformation("Set Auto Live Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    await _libraryWorkspaceActionLogic.SetAvailable(syncActionPayload);
                    await _libraryWorkspaceActionLogic.ChangeMLStatus(syncActionPayload);
                    break;
                case enWorkspaceAction.TAKEDOWN:
                //case enWorkspaceAction.UNDO_TAKEDOWN:
                case enWorkspaceAction.RESTRICT:
                // case enWorkspaceAction.UNDO_RESTRICT:
                case enWorkspaceAction.SET_LIVE:
                    _logger.LogInformation("Set Live Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    await _libraryWorkspaceActionLogic.ChangeMLStatus(syncActionPayload);
                    break;
                case enWorkspaceAction.ARCHIVE_TRACK:
                    _logger.LogInformation("Archive Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    result = await _unitOfWork.TrackOrg.ArchiveTrackAlbum(syncActionPayload);
                    break;
                case enWorkspaceAction.UNDO_ARCHIVE_TRACK:
                    _logger.LogInformation("Undo Archive Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    result = await _unitOfWork.TrackOrg.ArchiveTrackAlbum(syncActionPayload);
                    break;
                case enWorkspaceAction.SET_BBC_OWNED:
                    _logger.LogInformation("Set BBC owned Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    await _unitOfWork.Workspace.AddOrgWorkspace(syncActionPayload, "Owned");
                    await _libraryWorkspaceActionLogic.Resync(syncActionPayload);
                    break;                   
                case enWorkspaceAction.REMOVE_BBC_OWNED:
                    _logger.LogInformation("Remove BBC owned Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    await _unitOfWork.Workspace.RemoveOrgWorkspace(syncActionPayload, "Owned");
                    await _libraryWorkspaceActionLogic.Resync(syncActionPayload);
                    break;
                case enWorkspaceAction.SET_BBC_EXCLUDE:
                case enWorkspaceAction.REMOVE_BBC_EXCLUDE:
                    await _orgExcludeLogic.OrgExclude(syncActionPayload);
                    break;
                case enWorkspaceAction.RESYNC:
                    await _unitOfWork.Workspace.SetRedownloadWorkspace(syncActionPayload);
                    await _libraryWorkspaceActionLogic.Resync(syncActionPayload);
                    _logger.LogInformation("RESYNC Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId,"Workspace Action");
                    break;
                case enWorkspaceAction.PAUSE:
                    _logger.LogInformation("Sync Pause Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    await _libraryWorkspaceActionLogic.PauseAction(syncActionPayload);
                    break;
                case enWorkspaceAction.CONTINUE:
                    _logger.LogInformation("Sync Continue Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                    await _libraryWorkspaceActionLogic.ContinueAction(syncActionPayload);
                    break;
                case enWorkspaceAction.RESTRICT_TRACK:
                case enWorkspaceAction.REMOVE_RESTRICT_TRACK:
                    if (syncActionPayload.type == "track")
                    {
                        _logger.LogInformation("Restrict Workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");
                        result = await _unitOfWork.TrackOrg.Restrict(syncActionPayload);
                    }
                    break;
                case enWorkspaceAction.UPDATE_MUSIC_ORIGIN:
                    await _libraryWorkspaceActionLogic.UpdateMusicOrigin(syncActionPayload);
                    await _libraryWorkspaceActionLogic.Resync(syncActionPayload);
                    _logger.LogInformation("Update Music origin workspace | WorkspaceId: {WorkspaceId}, UserId:{UserId}, Module:{Module}", syncActionPayload.ids[0], syncActionPayload.userId, "Workspace Action");                    
                    break;
                default:
                    break;
            }
            return Ok(result);
        }
        [HttpPost("CheckServices")]
        public async Task<IActionResult> CheckServices()
        {
            return Ok(await _elasticLogic.GetServiceStatus());
        }

        [HttpPost("RestartServices")]
        public async Task<IActionResult> RestartServices()
         {
            await _elasticLogic.ResetServiceLogger();
            return Ok("Deleted");
        }
    }
}