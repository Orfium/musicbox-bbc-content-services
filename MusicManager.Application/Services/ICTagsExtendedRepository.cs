using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ICTagsExtendedRepository: IGenericRepository<c_tag_extended>
    {
        Task<List<c_tag_extended>> GetActiveRules(int id);
        Task<int> ChangeRuleStatus(int id, int status);
        Task<int> DeleteRule(int id);
        Task<c_tag_extended> SaveCtagExtended(c_tag_extended c_tag);
        Task<int> UpdateCtagExtended(c_tag_extended ctag_extended);
        Task<c_tag_extended> GetRuleByTrackId(string trackId);
    }
}
