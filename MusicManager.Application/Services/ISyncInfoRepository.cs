using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ISyncInfoRepository : IGenericRepository<sync_info>
    {
        Task<sync_info> GetLastSyncRecord(string type, Guid workspaceId);
    }
}
