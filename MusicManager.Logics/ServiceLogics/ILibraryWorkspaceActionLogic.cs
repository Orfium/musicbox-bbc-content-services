using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface ILibraryWorkspaceActionLogic 
    {
        Task SetAvailable(SyncActionPayload syncActionPayload);
        Task SetLive(SyncActionPayload syncActionPayload);
        Task Resync(SyncActionPayload syncActionPayload);
        Task PauseAction(SyncActionPayload pauseActionPayload);
        Task ContinueAction(SyncActionPayload pauseActionPayload);
        Task SetArchive(SyncActionPayload syncActionPayload);
        Task ChangeMLStatus(SyncActionPayload syncActionPayload);
        Task UpdateMusicOrigin(SyncActionPayload syncActionPayload);
        Task<library_org> CheckAndInsertLibraryOrg(Guid libraryId, Guid? workspaceId, string orgId, int userId, enMLStatus _enMLStatus);

    }
}
