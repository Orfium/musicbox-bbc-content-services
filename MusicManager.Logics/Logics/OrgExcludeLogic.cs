using Microsoft.EntityFrameworkCore;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class OrgExcludeLogic: IOrgExcludeLogic
    {
        private readonly IUnitOfWork _unitOfWork;     

        public OrgExcludeLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;          
        }

        public async Task<int> OrgExclude(SyncActionPayload syncActionPayload)
        {           
            foreach (var item in syncActionPayload.ids)
            {
                org_exclude org_exclude = await _unitOfWork.OrgExclude.GetByRefIdAndType(Guid.Parse(item) ,syncActionPayload.type);

                if (org_exclude == null && syncActionPayload.action == "SET_BBC_EXCLUDE")
                {
                    await _unitOfWork.OrgExclude.Add(new org_exclude()
                    {
                        created_by = int.Parse(syncActionPayload.userId),
                        date_created = DateTime.Now,
                        organization = syncActionPayload.orgid,
                        item_type = syncActionPayload.type,
                        ref_id = new Guid(item)
                    });
                }
                else if (org_exclude!=null && syncActionPayload.action == "REMOVE_BBC_EXCLUDE")
                {
                    _unitOfWork.OrgExclude.Remove(org_exclude);
                }
            }
            return await _unitOfWork.Complete();
        }
    }
}
