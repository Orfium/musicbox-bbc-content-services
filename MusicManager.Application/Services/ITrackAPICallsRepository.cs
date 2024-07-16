using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ITrackAPICallsRepository : IGenericRepository<log_track_api_calls>
    {
        Task<log_track_api_calls> SaveTrackAPICall(log_track_api_calls log_Track_Api_Calls);
    }
}
