using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MusicManager.Core.Payload;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PplLabelController : ControllerBase
    {
        private readonly IPplLabelLogic _pplLogic;

        public PplLabelController(IPplLabelLogic pplLogic)
        {
            _pplLogic = pplLogic;
        }

        [HttpPost("createPplLabel")]
        public async Task<IActionResult> AddLabel(PplLabelPayload pplLabel)
        {
            try
            {
                await _pplLogic.CreateLabel(pplLabel);
                return Ok(pplLabel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("editPplLabel")]
        public async Task<IActionResult> EditLabel(PplLabelPayload pplLabel)
        {
            try
            {
                await _pplLogic.EditLabel(pplLabel);
                return Ok(pplLabel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
