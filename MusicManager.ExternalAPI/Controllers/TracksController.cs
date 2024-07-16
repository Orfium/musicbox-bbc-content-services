using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MusicManager.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicManager.ExternalAPI.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    public class TracksController : ControllerBase
    {
         IMLMasterTrackRepository _track;

        public TracksController(IMLMasterTrackRepository track)
        {
            _track = track;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetTrackByTrackId(Guid trackId)
        {
            var track = await _track.GetElasticTrackById(trackId);
            return Ok(track);
        }
    }
}
