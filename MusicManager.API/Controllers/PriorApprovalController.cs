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
    public class PriorApprovalController : ControllerBase
    {
        private readonly IPriorApprovalLogic _priorApproval;

        public PriorApprovalController(IPriorApprovalLogic priorApproval)
        {
            _priorApproval = priorApproval;
        }

        [HttpPost("save")]
        public async Task<IActionResult> AddPriorApproval(PriorApprovalPayload priorApprovalPayload)
        {
            await _priorApproval.CreatePriorApproval(priorApprovalPayload);
            return Ok(priorApprovalPayload);
        }


        [HttpPost("edit")]
        public async Task<IActionResult> EditPriorApproval(PriorApprovalPayload priorApprovalPayload)
        {
            await _priorApproval.UpdatePriorApproval(priorApprovalPayload);
            return Ok(priorApprovalPayload);
        }
    }
}
