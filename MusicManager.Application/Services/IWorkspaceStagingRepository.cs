using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IWorkspaceStagingRepository : IGenericRepository<staging_workspace>
    {
        int Truncate();
        Task<int> BulkInsert(List<staging_workspace> stagingWorkspaces);
    }
}
