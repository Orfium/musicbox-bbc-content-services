using Elasticsearch.Util;
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
    public class PplLabelLogic : IPplLabelLogic
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;

        public PplLabelLogic(IUnitOfWork unitOfWork, IOptions<AppSettings> appSettings)
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
        }
        public async Task CreateLabel(PplLabelPayload pplPayload)
        {
            member_label ml = new member_label
            {
                date_created = DateTime.Now,
                label = pplPayload.label.ReplaceSpecialCodes(),
                member = pplPayload.member.ReplaceSpecialCodes(),
                mlc = pplPayload.mlc.ReplaceSpecialCodes(),
                source = "User",
                created_by = int.Parse(pplPayload.userId)                
            };

             await _unitOfWork.MemberLabel.InsertManualLabel(ml);
                    
        }

        public async Task EditLabel(PplLabelPayload pplPayload)
        {
            member_label ml = new member_label
            {
                id= pplPayload.id,
                date_created = DateTime.Now,
                label = pplPayload.label.ReplaceSpecialCodes(),
                member = pplPayload.member.ReplaceSpecialCodes(),
                mlc = pplPayload.mlc.ReplaceSpecialCodes(),
                source = "User",
                date_last_edited = DateTime.Now,
                last_edited_by = int.Parse(pplPayload.userId)
            };

            await _unitOfWork.MemberLabel.UpdateLabel(ml);           
        }

    }
}
