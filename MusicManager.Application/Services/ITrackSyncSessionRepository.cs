using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ITrackSyncSessionRepository : IGenericRepository<log_track_sync_session>
    {
        Task<log_track_sync_session> SaveTrackSyncSession(log_track_sync_session logTrackSyncSession);
        Task<int> UpdateTrackSyncSession(log_track_sync_session logTrackSyncSession);      
      
    }
}
