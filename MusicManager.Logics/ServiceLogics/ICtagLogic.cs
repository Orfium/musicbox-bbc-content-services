using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public interface ICtagLogic
    {
        Task AddCTag(CTagPayload ctagPayload);
        Task<ClearanceCTags> CheckPRSCTag(PrsPayload payload, MLTrackDocument mLTrackDocument);
        Task AddCTagExtended(CTagExtendedPayload ctagPayload, enCtagRuleStatus enCtagRuleStatus);
        Task<c_tag_extended> AddPPLCTagExtended(CTagExtendedPayload ctagPayload, string isrc);
        IEnumerable<c_tag_extended> GetCtagExtendedByCTagId(int cTagId);
        Task UpdateCTagExtended(CTagExtendedPayload ctagPayload);
        Task UpdateCTag(CTagPayload ctagPayload);
        Task<int> ChangeStatus(CTagArchivePayload ctagPayload);
        Task<int> ChangeRuleStatus(CTagArchivePayload ctagPayload);
        Task<int> DeleteRule(CTagDeletePayload ctagPayload);
        //Task<MLMasterTrack> CheckMLMasterTrackFroPRS(PrsPayload payload);       
        Task<List<ExplicitCTag>> CheckExplicitCtag(Guid[] trackIds, int cTagId);
        Task<List<IndicateCTag>> CheckDynamicDisplayCtag(Guid[] trackIds, int cTagId);
        Task<c_tag_extended> GetCtagByTrackId(string trackId);
        Task<CtagRuleCheck> CheckRules(MLTrackDocument mLTrackDocument, int cTagId);
        Task<PRSUpdateReturn> UpdatePRSforTrack(Guid? mlTrackId, MLTrackDocument mLTrackDocument, List<c_tag> c_Tags,bool charted,bool index = true);
        Task<List<IndicateCTagWithRule>> GetCtagRuleListByTrackIdAndCtagId(Guid trackId, List<int> cTagIds);   
        ClearanceCTags GetPRSWorkDetailsByTunecode(string tunecode);
    }
}
