using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MusicManager.Core.Payload;
using MusicManager.Logics.ServiceLogics;
using MusicManager.Playout.Models.Signiant;
using System;
using System.Threading.Tasks;

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayoutController : ControllerBase
    {
        public readonly IPlayoutLogic _playoutLogic;
        private readonly ILogger<PlayoutController> _logger;

        public PlayoutController(IPlayoutLogic playoutLogic,
            ILogger<PlayoutController> logger)
        {
            _playoutLogic = playoutLogic;
            _logger = logger;
        }

        [HttpPost("deleteTracks")]
        public async Task<IActionResult> DeletePlayoutTracks(PlayoutTrackIdPayload tracks)
        {
            var result = await _playoutLogic.DeletePlayOutTracks(tracks);
            return Ok(result);
        }

        [HttpPost("createPublish")]
        public async Task<IActionResult> CreateAndPublishPlayoutTracks(PlayoutPayload playout)
        {
            var result = await _playoutLogic.CreatePlayOut(playout, enPlayoutAction.CREATE_AND_PUBLISH);
            return Ok(result);
        }

        [HttpPost("publishTracks")]
        public async Task<IActionResult> PublishPlayoutTracks(PlayoutPayload playout)
        {
            var result = await _playoutLogic.CreatePlayOut(playout, enPlayoutAction.PUBLISH);
            return Ok(result);
        }

        [HttpPost("addTracks")]
        public async Task<IActionResult> AddPlayoutTracks(AddPlayoutPayload playout)
        {
            var res = await _playoutLogic.AddTracksToPlayOut(playout);
            if (res > 0)
                return Ok(res);
            else
            {
                return Ok();
            }
        }

        [HttpGet("GetTest")]
        public async Task<IActionResult> GetTest()
        {
            return Ok("success V1");
        }

        [HttpPost("DownloadPlayoutXML")]
        public async Task<IActionResult> DownloadPlayoutXML(PlayoutXMlDownloadPayload playoutXMlDownloadPayload)
        {
            try
            {
                if (playoutXMlDownloadPayload.outputType == enPlayoutDownloadType.XML.ToString())
                {
                    var result = await _playoutLogic.DownloadPlayoutXML(playoutXMlDownloadPayload);
                    _logger.LogInformation("DownloadPlayoutXML | RequestObject: {RequestObject} , Module: {Module}", playoutXMlDownloadPayload, "Playout");
                    return File(result.Content, "application/octet-stream", $"{System.Net.WebUtility.UrlEncode(result.FileName)}");
                }
                else {
                    var result = await _playoutLogic.DownloadPlayoutXML_ZIP(playoutXMlDownloadPayload);
                    _logger.LogInformation("DownloadPlayoutXML | RequestObject: {RequestObject} , Module: {Module}", playoutXMlDownloadPayload, "Playout");
                    return File(result, "application/octet-stream", $"PlayoutXMLs.zip");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadPlayoutXML | RequestObject: {RequestObject} , Module: {Module}", playoutXMlDownloadPayload, "Playout");
                return BadRequest(ex);
            }
        }

        [HttpPost("SigniantReply/{requestId}")]
        public async Task<IActionResult> SigniantReply([FromRoute] Guid requestId, [FromBody] SigniantReplyResponse response)
        {
            try
            {
                await _playoutLogic.UpdateSigniantReplay(requestId, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SigniantReply | RequestId: {RequestId} , Module: {Module}", requestId, "Playout");
                return BadRequest();
            }
        }

        [HttpPost("SigniantFault/{requestId}")]
        public async Task<IActionResult> SigniantFault([FromRoute] Guid requestId, [FromBody] SigniantFaultResponse response)
        {
            try
            {
                await _playoutLogic.UpdateSigniantFault(requestId, response);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SigniantFault | RequestId: {RequestId} , Module: {Module}", requestId, "Playout");
                return BadRequest();
            }
        }

        [HttpPost("Restart/{playoutId}")]
        public async Task<IActionResult> RestartPlayout([FromRoute] int playoutId,  string userId)
        {
            try
            {
                _logger.LogInformation("RestartPlayout | playoutId: {playoutId}, userId:  {userId}, Module: {Module}", playoutId, userId, "Playout");
                return Ok(await _playoutLogic.RestartPlayout(playoutId));               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RestartPlayout | playoutId: {playoutId}, userId:  {userId}, Module: {Module}", playoutId, userId, "Playout");
                return BadRequest();
            }
        }
    }
}
