using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Logics;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Threading.Tasks;

namespace MLCheckAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PRSApiController : ControllerBase
    {
        private readonly ICtagLogic _ctagLogic;
        private readonly IElasticLogic _elasticLogic;

        public PRSApiController(ICtagLogic ctagLogic,
            IElasticLogic elasticLogic)
        {
            _ctagLogic = ctagLogic;
            _elasticLogic = elasticLogic;
        }
        [HttpPost("SearchByTunecode")]
        public IActionResult SearchByTunecode(string tunecode)
        {
            //PRSFull pRSFull = _ctagLogic.SearchWorkByTuneCode(tunecode);

            //if (!string.IsNullOrEmpty(pRSFull.errorMessage))
            //    return BadRequest(pRSFull.errorMessage);

            return Ok();
        }

        [HttpPost("SearchByDHTrackId")]
        public async Task<IActionResult> SearchByDHTrackId(string trackId)
        {
            //MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocByDhTrackId(Guid.Parse(trackId));
            //if (mLTrackDocument == null)
            //    return NoContent();

            //PRSFull pRSFull = await _ctagLogic.SearchPRSForMLTrack(mLTrackDocument);

            //if (!string.IsNullOrEmpty(pRSFull.errorMessage))
            //    return BadRequest(pRSFull.errorMessage);

            return Ok();
        }
    }
}
