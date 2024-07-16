using MusicManager.Application.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application
{
    public interface IUnitOfWork
    {
        IWorkspaceService Workspace { get; }        
        ILibraryRepository Library { get; }       
        IWorkspaceStagingRepository WorkspaceStaging { get; }
        ILibraryStagingRepository LibraryStaging { get; }
        IMetadataAPIRepository MetadataAPI { get; }
        IMusicAPIRepository MusicAPI { get; }
        ITrackAPIResultsRepository TrackAPIResults { get; }
        ITrackAPICallsRepository TrackAPICalls { get; }
        ITrackSyncSessionRepository TrackSyncSession { get; }
        ILogElasticTrackChangeRepository LogElasticTrackChange { get; }
        ITagRepository Tag { get; }       
        ITagTypeRepository TagType { get; }
        ITrackOrgRepository TrackOrg { get; }
        IWorkspaceLibTracksTobeSyncedRepository WorkspaceLibTracksTobeSynced { get; }
        IMLMasterTrackRepository MLMasterTrack { get; }        
        IAlbumRepository Album { get; }
        ISyncInfoRepository SyncInfo { get; }
        IUploadTrackRepository UploadTrack { get; }
        IUploadAlbumRepository UploadAlbum { get; }
        ICTagsRepository CTags { get; }
        IOrgExcludeRepository OrgExclude { get; }
        ICTagsExtendedRepository CTagsExtended { get; }
        IMemberLabelRepository MemberLabel { get; }
        IPlayoutRepository PlayOut { get; }
        IUploadSessionRepository UploadSession { get; }
        IActionLoggerRepository ActionLogger { get; }
        IUserRepository User { get; }
        IPriorApprovalRepository PriorApproval { get; }
        IAlbumAPIResultsRepository AlbumAPIResults { get; }
        IAlbumAPICallsRepository AlbumAPICalls { get; }
        IAlbumSyncSessionRepository AlbumSyncSession { get; }
        ILogSyncTimeRepository logSyncTime { get; }
        ITunecodeIsrcRepository TunecodeIsrc { get; }
        IChartRepository Chart { get; }

        Task<int> Complete();
        void ResetContext();
        void AutoDetectChangesEnable();
        void AutoDetectChangesDisable();       
    }
}
