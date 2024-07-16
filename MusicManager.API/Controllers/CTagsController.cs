using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Logics;
using MusicManager.Logics.ServiceLogics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CTagsController : ControllerBase
    {
        private readonly ICtagLogic _ctagLogic;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IElasticLogic _elasticLogic;
        private readonly ILogger<PlayoutLogic> _logger;

        public CTagsController(ICtagLogic ctagLogic, IOptions<AppSettings> appSettings, 
            IElasticLogic elasticLogic,
            ILogger<PlayoutLogic> logger)
        {
            _ctagLogic = ctagLogic;
            _appSettings = appSettings;
            _elasticLogic = elasticLogic;
            _logger = logger;
        }

        [HttpPost("saveCTag")]
        public async Task<IActionResult> AddCtag(CTagPayload ctagPayload)
        {
            await _ctagLogic.AddCTag(ctagPayload);
            return Ok(ctagPayload);
        }

        [HttpPost("updateCTag")]
        public async Task<IActionResult> UpdateCtag(CTagPayload ctagPayload)
        {
            await _ctagLogic.UpdateCTag(ctagPayload);
            return Ok(ctagPayload);
        }

        [HttpPost("ctagStatus")]
        public async Task<IActionResult> ChangeMainCtagStatus(CTagArchivePayload ctagPayload)
        {
            var status = await _ctagLogic.ChangeStatus(ctagPayload);
            return Ok(status);
        }

        [HttpPost("ruleStatus")]
        public async Task<IActionResult> ChangeCtagRuleStatus(CTagArchivePayload ctagPayload)
        {
            var status = await _ctagLogic.ChangeRuleStatus(ctagPayload);
            return Ok(status);
        }

        [HttpPost("deleteRule")]
        public async Task<IActionResult> DeleteRule(CTagDeletePayload ctagPayload)
        {
            var status = await _ctagLogic.DeleteRule(ctagPayload);
            return Ok(status);
        }

        [HttpPost("deleteTrackRule")]
        public async Task<IActionResult> DeleteTrackRule(CTagTrackDeletePayload ctagPayload)
        {
            int count = 0;
            foreach (var trackId in ctagPayload.trackIds)
            {
                var ruleToDelete = await _ctagLogic.GetCtagByTrackId(trackId);
                if (ruleToDelete != null)
                {
                    CTagDeletePayload deletePayload = new CTagDeletePayload
                    {
                        ids = new List<int>() { ruleToDelete.id }
                    };
                    count += await _ctagLogic.DeleteRule(deletePayload);
                }
            }
            return Ok(count);
        }

        [HttpPost("removeExplicitRule")]
        public async Task<IActionResult> removeExplicitRule(CTagRuleDeletePayload cTagRuleDeletePayload)
        {
            int deleteCount = 0;
            foreach (CTagRuleListPayload item in cTagRuleDeletePayload.ctagPayload)
            {
                if(item.cTagIds.Contains(0))
                {
                    List<IndicateCTagWithRule> indicateCTagWithRules = await _ctagLogic.GetCtagRuleListByTrackIdAndCtagId(item.trackId, new List<int>() { _appSettings.Value.ExplicitCtagId });
                    if (indicateCTagWithRules?.Count() > 0 && indicateCTagWithRules[0].ruleDetails != null)
                    {
                      deleteCount =  await _ctagLogic.DeleteRule(new CTagDeletePayload() { ids = new List<int>() { indicateCTagWithRules[0].ruleDetails.ruleId }, userId = cTagRuleDeletePayload.userId });
                    }
                }
                else if (item.cTagIds.Contains(_appSettings.Value.ExplicitCtagId)) {
                    List<IndicateCTagWithRule> indicateCTagWithRules = await _ctagLogic.GetCtagRuleListByTrackIdAndCtagId(item.trackId, new List<int>() { _appSettings.Value.ExplicitCtagId });
                    if (indicateCTagWithRules?.Count() > 0 && indicateCTagWithRules[0].ruleDetails != null)
                    {
                        deleteCount = await _ctagLogic.DeleteRule(new CTagDeletePayload() { ids = new List<int>() { indicateCTagWithRules[0].ruleDetails.ruleId }, userId = cTagRuleDeletePayload.userId });
                    }                   
                }                 
            }  
            return Ok(deleteCount);
        }

        [HttpPost("SaveCTagExtended")]
        public async Task<IActionResult> AddCtagExtended(CTagExtendedPayload ctagPayload)
        {
            await _ctagLogic.AddCTagExtended(ctagPayload, enCtagRuleStatus.Created);
            return Ok(ctagPayload);
        }

        [HttpPost("SaveExpicitCTagExtended")]
        public async Task<IActionResult> SaveExpicitCTagExtended(CTagExtendedPayload ctagPayload)
        {
            ctagPayload.c_tag_id = _appSettings.Value.ExplicitCtagId;            

            await _ctagLogic.AddCTagExtended(ctagPayload, enCtagRuleStatus.Active);
            return Ok(ctagPayload);
        }

        [HttpPost("SavePPLCTagRule")]
        public async Task<IActionResult> SavePPLCTagRule(CTagExtendedPayload ctagPayload)
        {
            MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocById(Guid.Parse(ctagPayload.track_id));
            if (mLTrackDocument == null || string.IsNullOrEmpty(mLTrackDocument.isrc))
                return NoContent();

            ctagPayload.c_tag_id = _appSettings.Value.PPLCtagId;
            
            return Ok(await _ctagLogic.AddPPLCTagExtended(ctagPayload, mLTrackDocument.isrc));
        }

        [HttpPost("updateCTagExtended")]
        public async Task<IActionResult> UpdateCtagExtended(CTagExtendedPayload ctagPayload)
        {
            await _ctagLogic.UpdateCTagExtended(ctagPayload);
            return Ok(ctagPayload);
        }      

        [HttpPost("GetCtagExtendedListByCTagId")]
        public IActionResult GetCtagExtendedByCTagId(int cTagId)
        {
            IEnumerable<c_tag_extended> c_tag_extended = _ctagLogic.GetCtagExtendedByCTagId(cTagId);
            return Ok(c_tag_extended);
        }

        [HttpPost("GetCTagResultsByTrackId")]
        public async Task<IActionResult> GetCTagResultsByTrackId(PrsPayload payload)
        {
            MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocById(Guid.Parse(payload.trackId));
            if (mLTrackDocument == null)
                return NoContent();

            ClearanceCTags clearanceCTags = await _ctagLogic.CheckPRSCTag(payload, mLTrackDocument);

            if (clearanceCTags.reqestedCtagGroup == 3 &&
                clearanceCTags.cTags.FirstOrDefault(a => a.id == (int)enCTagTypes.PRS_MCPS_OWNERSHIP)?.result == null) {
                _logger.LogWarning("Clearance CTag Search Error {track_id}", payload.trackId);
            }

            if (payload.ctagId != null)
            {
                CTagOrg cTagOrg = clearanceCTags.cTags.FirstOrDefault(a => a.id == payload.ctagId);
                if (cTagOrg != null)
                {
                    clearanceCTags.cTags = clearanceCTags.cTags.Where(a => a.groupId == cTagOrg.groupId).ToList();                    
                }
            }

            _logger.LogInformation("GetCtagExtendedListByCTagId | TrackId:{trackId} ,CtagId:{CtagId} , clearanceCTags :{Object}, Module: {Module}", payload.trackId,payload.ctagId, JsonConvert.SerializeObject(clearanceCTags), "Clearance Ctags");

            return Ok(clearanceCTags);
        }

        [HttpPost("CheckExplicitCtag")]
        public async Task<IActionResult> CheckExplicitCtag(Guid[] trackIds)
        {
            var result = await _ctagLogic.CheckExplicitCtag(trackIds, _appSettings.Value.ExplicitCtagId);
            return Ok(result);
        }

        [HttpPost("CheckDynamicDisplayCtag")]
        public async Task<IActionResult> CheckDynamicDisplayCtag(Guid[] trackIds)
        {
            var result = await _ctagLogic.CheckDynamicDisplayCtag(trackIds, _appSettings.Value.ExplicitCtagId);
            return Ok(result);
        }

        [HttpPost("GetCtagRuleListByTrackIdAndCtagId")]
        public async Task<IActionResult> GetCtagRuleListByTrackIdAndCtagId(CTagRuleListPayload payload)
        {
            var result = await _ctagLogic.GetCtagRuleListByTrackIdAndCtagId(payload.trackId, payload.cTagIds);
            return Ok(result);
        }


    }
}
