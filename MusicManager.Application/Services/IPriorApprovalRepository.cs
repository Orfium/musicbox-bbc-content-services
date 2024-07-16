using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IPriorApprovalRepository: IGenericRepository<prior_approval_work>
    {
        Task<prior_approval_work> SavePriorApproval(prior_approval_work prior_Approval);
        Task<int> UpdatePriorApproval(prior_approval_work prior_Approval);
    }
}
