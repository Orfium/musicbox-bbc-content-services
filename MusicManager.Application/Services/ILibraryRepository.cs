using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ILibraryRepository : IGenericRepository<library>
    {
        void SyncLibraries(int UserId);
        Task<library_org> SaveLibraryOrg(library_org library_Org);
        Task<int> UpdateLibraryOrg(library_org library_Org);
        Task<library_org> GetLibraryByOrgId(Guid libraryId, string orgId);
        Task<IEnumerable<library>> GetLibraryListByWorkspaceId(Guid workspaceId);
        Task<IEnumerable<library_org>> GetOrgLibraryListByWorkspaceId(Guid workspaceId,string orgId);
        Task<library> GetById(Guid libraryId);
        Task<IEnumerable<library>> GetNewLiveLibrariesAfterLive();
        Task<IEnumerable<library>> GetNewAvailableLibrariesAfterLive();
        Task<IEnumerable<library>> GetNewDistinctWorkspacesAfterLive();
        Task<int> UpdateLibraryTrackCount(library library);
    }
}
