using Microsoft.EntityFrameworkCore;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.PrsModels;
using MusicManager.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private MLContext _context;

        public UnitOfWork(MLContext music_ManagerContext,
            ILibraryRepository libraryService,
            IWorkspaceService workspaceService,
            IWorkspaceStagingRepository workspaceStagingRepository,
            ILibraryStagingRepository libraryStagingRepository,
            IMetadataAPIRepository metadataAPIRepository,
            ITrackAPIResultsRepository trackAPIResultsRepository,
            ITrackAPICallsRepository trackAPICallsRepository,
            ITrackSyncSessionRepository trackSyncSessionRepository,
            ILogElasticTrackChangeRepository logElasticTrackChangeRepository,
            ITagRepository tagRepository,            
            ITagTypeRepository tagTypeRepository,
            IMusicAPIRepository musicAPIRepository,
            ITrackOrgRepository trackOrgRepository,
            IWorkspaceLibTracksTobeSyncedRepository workspaceLibTracksTobeSyncedRepository,
            IMLMasterTrackRepository mLMasterTrackRepository,
            IAlbumRepository albumRepository,
            ISyncInfoRepository syncInfoRepository,
            IUploadTrackRepository uploadTrackRepository,
            IUploadAlbumRepository uploadAlbumRepository,
            ICTagsRepository ctagsRepository,
            IOrgExcludeRepository orgExcludeRepository,
            ICTagsExtendedRepository cTagsExtendedRepository,
            IMemberLabelRepository memberLabelRepository,
            IPlayoutRepository playoutRepository,
            IUploadSessionRepository uploadSessionRepository,
            IActionLoggerRepository actionLoggerRepository,
            IUserRepository userRepository,
            IPriorApprovalRepository priorApproval,
            IAlbumAPICallsRepository albumAPICallsRepository,
            IAlbumAPIResultsRepository albumAPIResultsRepository,
            IAlbumSyncSessionRepository albumSyncSessionRepository,
            ILogSyncTimeRepository logSyncTimeRepository,
            ITunecodeIsrcRepository tunecodeIsrcRepository,
            IChartRepository chartRepository
            )
        {
            _context = music_ManagerContext;
            Library = libraryService;
            Workspace = workspaceService;
            WorkspaceStaging = workspaceStagingRepository;
            LibraryStaging = libraryStagingRepository;
            MetadataAPI = metadataAPIRepository;
            TrackAPIResults = trackAPIResultsRepository;
            TrackAPICalls = trackAPICallsRepository;
            TrackSyncSession = trackSyncSessionRepository;
            LogElasticTrackChange = logElasticTrackChangeRepository;
            Tag = tagRepository;           
            TagType = tagTypeRepository;
            MusicAPI = musicAPIRepository;
            TrackOrg = trackOrgRepository;
            WorkspaceLibTracksTobeSynced = workspaceLibTracksTobeSyncedRepository;
            MLMasterTrack = mLMasterTrackRepository;
            Album = albumRepository;
            SyncInfo = syncInfoRepository;
            UploadTrack = uploadTrackRepository;
            UploadAlbum = uploadAlbumRepository;
            CTags = ctagsRepository;
            OrgExclude = orgExcludeRepository;
            CTagsExtended = cTagsExtendedRepository;
            MemberLabel = memberLabelRepository;
            PlayOut = playoutRepository;
            UploadSession = uploadSessionRepository;
            ActionLogger = actionLoggerRepository;
            User = userRepository;
            PriorApproval = priorApproval;
            AlbumAPIResults = albumAPIResultsRepository;
            AlbumAPICalls = albumAPICallsRepository;
            AlbumSyncSession = albumSyncSessionRepository;
            logSyncTime = logSyncTimeRepository;
            TunecodeIsrc = tunecodeIsrcRepository;
            Chart = chartRepository;
        }

        public ILibraryRepository Library { get; }
        public ITunecodeIsrcRepository TunecodeIsrc { get; }

        public IWorkspaceService Workspace { get; }
        public IWorkspaceStagingRepository WorkspaceStaging { get; }
        public ILibraryStagingRepository LibraryStaging { get; }
        public IMetadataAPIRepository MetadataAPI { get; }
        public ITrackAPIResultsRepository TrackAPIResults { get; }
        public ITrackAPICallsRepository TrackAPICalls { get; }
        public ITrackSyncSessionRepository TrackSyncSession { get; }
        public ILogElasticTrackChangeRepository LogElasticTrackChange { get; }
        public ITagRepository Tag { get; }
        public ITagTypeRepository TagType { get; }
        public IMusicAPIRepository MusicAPI { get; }
        public ITrackOrgRepository TrackOrg { get; }
        public IWorkspaceLibTracksTobeSyncedRepository WorkspaceLibTracksTobeSynced { get; }
        public IMLMasterTrackRepository MLMasterTrack { get; }
        public IAlbumRepository Album { get; }
        public ISyncInfoRepository SyncInfo { get; }
        public IUploadTrackRepository UploadTrack { get; }
        public IUploadAlbumRepository UploadAlbum { get; }
        public ICTagsRepository CTags { get; }
        public IOrgExcludeRepository OrgExclude { get; }
        public ICTagsExtendedRepository CTagsExtended { get; }
        public IMemberLabelRepository MemberLabel { get; }
        public IPlayoutRepository PlayOut { get; }
        public IUploadSessionRepository UploadSession { get; }
        public IActionLoggerRepository ActionLogger { get; }
        public IUserRepository User { get; }
        public IPriorApprovalRepository PriorApproval { get; }
        public IAlbumAPIResultsRepository AlbumAPIResults { get; }
        public IAlbumAPICallsRepository AlbumAPICalls { get; }
        public IAlbumSyncSessionRepository AlbumSyncSession { get; }
        public ILogSyncTimeRepository logSyncTime { get; }
        public IChartRepository Chart { get; }

        public void AutoDetectChangesDisable()
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        public void AutoDetectChangesEnable()
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        public async Task<int> Complete()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void ResetContext()
        {
            _context.Dispose();
        }
    }
}
