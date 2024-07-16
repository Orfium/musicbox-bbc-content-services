using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ILogSyncTimeRepository : IGenericRepository<log_sync_time>
    {
        Task<long> Save(log_sync_time log_Sync_Time);
        Task<int> UpdateLogSyncTime(log_sync_time log_Sync_Time);
        Task<bool> CheckServiceStatus(Guid workspaceId);
    }
}
