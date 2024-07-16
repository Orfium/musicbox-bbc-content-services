using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ITrackAPIResultsRepository : IGenericRepository<log_track_api_results>
    {
        Task<int> BulkInsert(List<log_track_api_results> log_Track_Api_Results);
        Task<int> Insert(log_track_api_results log_Track_Api_Results);
        Task DeleteAllBySessionId(int sessionId);
    }
}
