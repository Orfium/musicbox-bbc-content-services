using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IWorkspaceService : IGenericRepository<workspace>
    {
        void SyncWorkspaces(int userId);
        Task<IEnumerable<workspace>> GetWorkspacesForSyncAsync(int retry = 2);
        Task<IEnumerable<workspace>> GetPriorityWorkspacesForSyncAsync();
        Task<IEnumerable<workspace>> GetMasterWorkspaceForSyncAsync(Guid masterWorkspaceId, int retries = 2);
        Task<int> UpdateWorkspace(workspace workspace);
        Task<int> UpdateLastSyncTime(workspace workspace);
        Task<workspace> GetWorkspaceById(Guid workspaceId);
        Task<bool> AddOrgWorkspace(SyncActionPayload syncActionPayload, string type);
        Task<bool> RemoveOrgWorkspace(SyncActionPayload syncActionPayload, string type);
        Task<int> AddOrgWorkspaceStatus(SyncActionPayload syncActionPayload, string type);       
        Task<enWorkspaceType> GetWorkspaceType(string wsId, string orgId);
        Task<workspace_org> SaveWorkspaceOrg(workspace_org workspace_Org);
        Task<int> UpdateWorkspaceOrg(workspace_org workspace_Org);
        Task<workspace_org> GetWorkspaceOrgByOrgId(Guid workspaceId,string orgId);
        Task<workspace_org> GetWorkspaceOrgById(Guid workspaceOrgId);
        Task<IEnumerable<workspace_org>> GetWorkspaceOrgs(string orgId, int mlStatusId);
        Task<IEnumerable<workspace_org>> GetWorkspaceOrgsByWorkspaceId(Guid workspaceId);
        Task<bool> CheckPause(Guid wsId);
        Task<int> WorkspacePause(SyncActionPayload pauseActionPayload);
        Task<int> WorkspaceContinue(SyncActionPayload pauseActionPayload);
        Task<int> UpdateDownloadStatus(SyncActionPayload pauseActionPayload, enLibWSDownloadStatus downloadStatus);
        Task<int> GetPreviosStatusFromPause(Guid wsId);
        Task<int> GetLiveWorkspaceCount();
        Task<int> GetAvlWorkspaceCount();
        Task<org_workspace> GetOrgWorkspaceByOrgId(string orgId, string workspace);
        Task<int> SetRestricted(SyncActionPayload syncActionPayload);
        Task<int> SetRedownloadWorkspace(SyncActionPayload syncActionPayload);
        Task<int> UpdateMusicOrigin(workspace_org workspace_Org);
        Task<int> GetWorkspaceActiveTrackCount(Guid workspaceId);
    }
}
