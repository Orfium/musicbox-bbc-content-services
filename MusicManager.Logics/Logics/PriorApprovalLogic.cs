using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class PriorApprovalLogic: IPriorApprovalLogic
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;

        public PriorApprovalLogic(IUnitOfWork unitOfWork, IOptions<AppSettings> appSettings)
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
        }

        public async Task CreatePriorApproval(PriorApprovalPayload priorApprovalPayload)
        {
            prior_approval_work paw = new prior_approval_work
            {
                artist = priorApprovalPayload.artist,
                broadcaster = priorApprovalPayload.broadcaster,
                composers = priorApprovalPayload.composers,
                created_by = Convert.ToInt32(priorApprovalPayload.userId),
                ice_mapping_code = priorApprovalPayload.ice_mapping_code,
                iswc = priorApprovalPayload.iswc,
                local_work_id = priorApprovalPayload.local_work_id,
                publisher = priorApprovalPayload.publisher,
                tunecode = priorApprovalPayload.tunecode,
                work_title = priorApprovalPayload.work_title,
                writers = priorApprovalPayload.writers,
                matched_dh_ids = priorApprovalPayload.matched_dh_ids,
                matched_isrc = priorApprovalPayload.matched_isrc,
                last_edited_by = Convert.ToInt32(priorApprovalPayload.userId),
            };

            await _unitOfWork.PriorApproval.SavePriorApproval(paw);
            log_user_action actionLog = new log_user_action
            {
                data_type = "",
                date_created = DateTime.Now,
                user_id = Convert.ToInt32(priorApprovalPayload.userId),
                org_id = priorApprovalPayload.org_id,
                data_value = "",
                action_id = (int)enActionType.CREATE_PRIOR_APPROVAL,
                ref_id = Guid.Empty, // ws id
                status = 1
            };
            await _unitOfWork.ActionLogger.Log(actionLog);
        }


        public async Task UpdatePriorApproval(PriorApprovalPayload priorApprovalPayload)
        {
            prior_approval_work paw = new prior_approval_work
            {
                id = Convert.ToInt64(priorApprovalPayload.id),
                artist = priorApprovalPayload.artist,
                broadcaster = priorApprovalPayload.broadcaster,
                composers = priorApprovalPayload.composers,
                created_by = Convert.ToInt32(priorApprovalPayload.userId),
                ice_mapping_code = priorApprovalPayload.ice_mapping_code,
                iswc = priorApprovalPayload.iswc,
                local_work_id = priorApprovalPayload.local_work_id,
                publisher = priorApprovalPayload.publisher,
                tunecode = priorApprovalPayload.tunecode,
                work_title = priorApprovalPayload.work_title,
                writers = priorApprovalPayload.writers,
                matched_dh_ids = priorApprovalPayload.matched_dh_ids,
                matched_isrc = priorApprovalPayload.matched_isrc,
                last_edited_by = Convert.ToInt32(priorApprovalPayload.userId),
            };

            await _unitOfWork.PriorApproval.UpdatePriorApproval(paw);
            log_user_action actionLog = new log_user_action
            {
                data_type = "",
                date_created = DateTime.Now,
                user_id = Convert.ToInt32(priorApprovalPayload.userId),
                org_id = priorApprovalPayload.org_id,
                data_value = "",
                action_id = (int)enActionType.UPDATE_PRIOR_APPROVAL,
                ref_id = Guid.Empty, // ws id
                status = 1
            };
            await _unitOfWork.ActionLogger.Log(actionLog);
        }
    }
}
