using Elasticsearch.DataMatching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MusicManager.Application.WebService;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Logics;
using MusicManager.Logics.ServiceLogics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrsController : ControllerBase
    {
        private readonly IProduct _product;
        private readonly IPrsRecording _recording;
        private readonly IWork _work;
        private readonly IPrsWorkDetails _workDetails;
        private readonly ICtagLogic _ctagLogic;
        private readonly IElasticLogic _elasticLogic;
        private readonly ILogger<PrsController> _logger;

        public PrsController( IProduct product, 
            IPrsRecording recording, 
            ILogger<PrsController> logger, 
            IWork work, 
            IPrsWorkDetails workDetails, 
            ICtagLogic ctagLogic,
            IElasticLogic elasticLogic
            )
        {
          
            _product = product;
            _recording = recording;
            _logger = logger;
            _work = work;
            _workDetails = workDetails;
            _ctagLogic = ctagLogic;
            _elasticLogic = elasticLogic;
        }

        [HttpPost("products/catno")]
        public IActionResult GetProductsByCatNo(string catNo)
        {
            try
            {
                var products = _product.GetProductByCatNo(catNo);
                return Ok(products);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.ToString());
                return new StatusCodeResult(500);
            }
           
        }

        [HttpPost("products/title")]
        public IActionResult GetProductsByTitle(string title)
        {
            var products =  _product.GetProductByTitle(title);
            return Ok(products);
        }

        [HttpPost("products/tunecode")]
        public IActionResult GetProductsByTuneCode(string tuneCode)
        {
            var products = _product.GetProductByTuneCode(tuneCode);
            return Ok(products);
        }

        [HttpPost("works/titles")]
        public IActionResult GetWorkssByTitles(string title)
        {
            string[] titles = { "Break Up Song", "Gangnam" };

            var works = _work.GetWorksFromTitle(titles);
            return Ok(works);
        }

        [HttpPost("works/tunecode")]
        public IActionResult GetWorkssByTuneCode(string tunecode)
        {
            var works = _work.GetWorksFromTuneCode(tunecode);
            return Ok(works);
        }

        [HttpPost("workdetails/tunecode")]
        public IActionResult GetWorkDetailsByTuneCode(string tunecode)
        {
            var works = _workDetails.GetWorkDetailsByTuneCode(tunecode);
            return Ok(works);
        }

        [HttpPost("recordings/isrc")]
        public IActionResult GetRecordingsByISRC(string isrc)
        {
            var recordings = _recording.GetRecordingByIsrc(isrc);
            return Ok(recordings);
        }

        [HttpPost("recordings/title-artist")]
        public IActionResult GetRecordingsByTitleArtist(string title, string artists)
        {
            var recordings = _recording.GetRecordingsByTitleArtist(title, artists);
            return Ok(recordings);
        }

        [HttpPost("recordings/title")]
        public IActionResult GetRecordingsByTitle(string title)
        {
            var recordings = _recording.GetRecordingsByTitle(title);
            return Ok(recordings);
        }

        [HttpPost("ManualMatch/{trackId}")]
        public async Task<IActionResult> ManualMatch([FromRoute] Guid trackId)
        {
            MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocByDhTrackId(trackId);
            if (mLTrackDocument == null)
                return NoContent();

            ClearanceCTags clearanceCTags = await _ctagLogic.CheckPRSCTag(new PrsPayload() { ctagId = 2}, mLTrackDocument);

            clearanceCTags.cTagMcpsOwner = TrackDocumentExtensions.GetCtagStatus(clearanceCTags, (int)enCTagTypes.PRS_MCPS_OWNERSHIP);
            clearanceCTags.cTagNorthAmerican = TrackDocumentExtensions.GetCtagStatus(clearanceCTags, (int)enCTagTypes.NORTH_AMERICAN_COPYRIGHT);

            _logger.LogInformation("PRS ManualMatch Track: {trackId}, External Work Tunecode: {ExternalTunecode}, Match summary: {ClearanceCTags}  | Module: {Module}",
                trackId, clearanceCTags.workTunecode, JsonConvert.SerializeObject(clearanceCTags), "PRS Manual Match");

            return Ok(clearanceCTags);
        }

        [HttpPost("ManualMatchByTunecode")]
        public async Task<IActionResult> ManualMatchByTunecode(ManualPRSUpdate manualPRSUpdate)
        {
            MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocByDhTrackId(Guid.Parse(manualPRSUpdate.dhTrackId));
            if (mLTrackDocument == null)
                return NoContent();

            mLTrackDocument.prs = manualPRSUpdate.tunecode;

            ClearanceCTags clearanceCTags = await _ctagLogic.CheckPRSCTag(new PrsPayload() { ctagId = 2 }, mLTrackDocument);

            clearanceCTags.cTagMcpsOwner = TrackDocumentExtensions.GetCtagStatus(clearanceCTags, (int)enCTagTypes.PRS_MCPS_OWNERSHIP);
            clearanceCTags.cTagNorthAmerican = TrackDocumentExtensions.GetCtagStatus(clearanceCTags, (int)enCTagTypes.NORTH_AMERICAN_COPYRIGHT);

            _logger.LogInformation("PRS ManualMatchByTunecode Track: {trackId}, Tunecode: {Tunecode}, External Work Tunecode: {ExternalTunecode}, User: {UserId}, OrgId: {OrgId}, Match summary: {ClearanceCTags}  | Module: {Module}",
                manualPRSUpdate.dhTrackId, manualPRSUpdate.tunecode, clearanceCTags.workTunecode, manualPRSUpdate.userId, manualPRSUpdate.orgId, JsonConvert.SerializeObject(clearanceCTags), "PRS Manual Match");
                        
            return Ok(clearanceCTags);
        }

    }
}
