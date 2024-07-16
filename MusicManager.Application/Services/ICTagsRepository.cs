using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ICTagsRepository: IGenericRepository<c_tag>
    {
        void AddMemberLabelData(member_label member_label);
        void AddPriorApprovalWork(prior_approval_work prior_approval_work);
        Task<int> ChangeStatus(int id, int status);
        Task<IEnumerable<c_tag>> GetAllActiveCtags();
        Task<c_tag> SaveCtag(c_tag c_tag);
        Task<int> UpdateCtag(c_tag c_tag);
        Task<c_tag> GetCtagById(int id);
        Task<c_tag> GetCtagByRuleId(int id);
        Task<IEnumerable<c_tag>> GetDynamicDisplayCtags();       
        Task<int> SavePrsSearchTime(log_prs_search_time log_prs_search_time);
        Task<string> GetTunecodeByISRC(string isrc);
        Task<int> UpdateCtagIndexStatus(c_tag_index_status cTagIndexStatus);
        Task<c_tag_index_status> GetCtagIndexStatusByType(string type);
        Task<IEnumerable<c_tag>> GetIndexedCtags();
        Task<c_tag> GetArchiveIndexedCtag();
        Task<bool> CheckPPLLabelExact(string label);
        Task<bool> CheckPPLLabelContains(string label);
    }
}
