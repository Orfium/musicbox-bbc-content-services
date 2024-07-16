using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public class SummaryController : ControllerBase
    {
        private readonly ILogLogic _logLogic;
        private readonly ILogger<SummaryController> _logger;

        public SummaryController(ILogLogic logLogic,
            ILogger<SummaryController> logger)
        {
            _logLogic = logLogic;
            _logger = logger;
        }

        [HttpPost("GetDailySyncSummary")]
        public async Task<IActionResult> GetDailySyncSummary()
        {
            try
            {
                SyncSummary syncSummary = await _logLogic.GetDailySyncSummaryCount("n2eu7wcxhyhmoj0fxgsf", DateTime.Now.AddDays(-1));
                return Ok(syncSummary);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "GetDailySyncSummary");
                return Ok(null);
            }
        }
    }
}
