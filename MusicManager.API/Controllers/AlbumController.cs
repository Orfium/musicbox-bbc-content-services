using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumController : ControllerBase
    {
        private readonly IAlbumLogic _albumLogic;

        public IOptions<AppSettings> _appSettings { get; }
        public IUnitOfWork _unitOfWork { get; }
        public IUploaderLogic _uploaderLogic { get; }
        public ILogger<AlbumController> _logger { get; }

        public AlbumController(IAlbumLogic albumLogic,
            IOptions<AppSettings> appSettings,
            IUnitOfWork unitOfWork,
            IUploaderLogic uploaderLogic,
            ILogger<AlbumController> logger
            )
        {
            _albumLogic = albumLogic;
            _appSettings = appSettings;
            _unitOfWork = unitOfWork;
            _uploaderLogic = uploaderLogic;
            _logger = logger;
        }

        [HttpPost("uploadAlbum")]
        public async Task<IActionResult> CreateAlbum(TrackUpdatePayload trackUpdatePayload)
        {
            try
            {
                var status = await _albumLogic.CreateAlbum(trackUpdatePayload);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAlbum {AlbumId}| Module: {Module} ", trackUpdatePayload.albumdata.dh_album_id, "Album");               
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("updateAlbum")]
        public async Task<IActionResult> UpdateAlbum(TrackUpdatePayload albumPayload)
        {
            try
            {
                await _albumLogic.UpdateAlbum(albumPayload);
                return Ok(albumPayload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAlbum {AlbumId}| Module: {Module} ", albumPayload.albumdata.dh_album_id, "Album");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("addTrackToAlbum")]
        public async Task<IActionResult> AddTrackToAlbum(AddTrackToAlbumPayload albumPayload)
        {
            try
            {
                var status = await _albumLogic.AddTracksToAlbum(albumPayload);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddTrackToAlbum {AlbumId}| Module: {Module} ", albumPayload.albumId, "Album");              
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("deleteAlbum")]
        public async Task<IActionResult> DeleteAlbum(DeleteAlbumPayload albumPayload)
        {
            try
            {
                //since this only upload tracks, assigning master workspace ID
                albumPayload.ws_id = _appSettings.Value.MasterWSId;
                var status = await _albumLogic.DeleteAlbum(albumPayload, enWorkspaceType.Master);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("GetAlbumForEdit")]
        public async Task<IActionResult> GetAlbumForEdit(AlbumkEditDeletePayload albumkEditDeletePayload)
        {
            try
            {
                DHTrackEdit dHTrackEdit = new DHTrackEdit();
                if(string.IsNullOrEmpty(albumkEditDeletePayload.workspaceId))
                {
                    albumkEditDeletePayload.workspaceId = _appSettings.Value.MasterWSId;
                }
                enWorkspaceType enWorkspaceType = await _unitOfWork.Workspace.GetWorkspaceType(albumkEditDeletePayload.workspaceId, albumkEditDeletePayload.orgId);
                dHTrackEdit.wsType = enWorkspaceType.ToString();

                if (enWorkspaceType == enWorkspaceType.External) // If it is External WS can't edit
                {
                    return Ok(dHTrackEdit);
                }
                
                dHTrackEdit = await _albumLogic.CreateEditAlbum(albumkEditDeletePayload);
                dHTrackEdit.wsType = enWorkspaceType.ToString();

                return Ok(dHTrackEdit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAlbumForEdit {AlbumId}| Module: {Module} ", albumkEditDeletePayload.albumId, "Album");                            
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ReorderAlbumTracks")]
        public async Task<IActionResult> ReorderAlbumTracks(TrackReorderPayload trackReorderPayload)
        {
            try
            {
                List<upload_track> uploadTracks = await _albumLogic.ReorderAlbumTracks(trackReorderPayload);

                _ = Task.Run(() => _uploaderLogic.UpdateDatahubTracksByUploadTracks(uploadTracks)).ConfigureAwait(false);                

                return Ok(uploadTracks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReorderAlbumTracks {AlbumId}| Module: {Module} ", trackReorderPayload.albumId, "Album");                        
                return BadRequest(ex.Message);
            }
        }
    }
}
