using Microsoft.Extensions.DependencyInjection;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Application.WebService;
using MusicManager.Infrastructure.Repository;
using MusicManager.Infrastructure.WebService;
using MusicManager.Logics.Logics;
using MusicManager.Logics.ServiceLogics;
using MusicManager.Playout.Build.MetadataMapping;

namespace MusicManager.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IWorkspaceService, WorkspaceRepository>();
            services.AddScoped<ILibraryRepository, LibraryRepository>();
            services.AddScoped<IWorkspaceStagingRepository, WorkspaceStagingRepository>();
            services.AddScoped<ILibraryStagingRepository, LibraryStagingRepository>();
            services.AddScoped<IMetadataAPIRepository, MetadataAPIRepository>();
            services.AddScoped<ISearchAPIRepository, SearchAPIRepository>();
            services.AddScoped<ITrackAPIResultsRepository, TrackAPIResultsRepository>();
            services.AddScoped<ITrackAPICallsRepository, TrackAPICallsRepository>();
            services.AddScoped<ITrackSyncSessionRepository, TrackSyncSessionRepository>();
            services.AddScoped<ILogElasticTrackChangeRepository, LogElasticTrackChangeRepository>();
            services.AddScoped<ITagRepository, TagRepository>();          
            services.AddScoped<ITagTypeRepository, TagTypeRepository>();
            services.AddScoped<IMusicAPIRepository, MusicAPIRepository>();
            services.AddScoped<ITrackOrgRepository, TrackOrgRepository>();
            services.AddScoped<IWorkspaceLibTracksTobeSyncedRepository, WorkspaceLibTracksTobeSyncedRepository>();
            services.AddScoped<IMLMasterTrackRepository, MLMasterTrackRepository>();
            services.AddScoped<IAlbumRepository, AlbumRepository>();
            services.AddScoped<ISyncInfoRepository, SyncInfoRepository>();
            services.AddScoped<IUploadTrackRepository, UploadTrackRepository>();
            services.AddScoped<IUploadAlbumRepository, UploadAlbumRepository>();
            services.AddScoped<ICTagsRepository, CTagsRepository>();
            services.AddScoped<ICTagsExtendedRepository, CTagsExtendedRepository>();
            services.AddScoped<IMemberLabelRepository, MemberLabelRepository>();
            services.AddScoped<IOrgExcludeRepository, OrgExcludeRepository>();
            services.AddScoped<IOrgExcludeLogic, OrgExcludeLogic>();
            services.AddScoped<ICtagLogic, CTagsLogic>();
            services.AddScoped<IAlbumLogic, AlbumLogic>();
            services.AddScoped<IPplLabelLogic, PplLabelLogic>();
            services.AddScoped<IPlayoutLogic, PlayoutLogic>();
            services.AddScoped<MusicManager.Application.Services.IAuthentication, MusicManager.Infrastructure.Repository.Authentication>();
            services.AddScoped<IProduct, Product>();
            services.AddScoped<IPrsRecording, PrsRecording>();
            services.AddScoped<IWork, Work>();
            services.AddScoped<IPrsWorkDetails, PrsWorkDetails>();
            services.AddScoped<IDHTrackSync, DHTrackSync>();
            services.AddScoped<IMlMasterTrackLogic, MlMasterTrackLogic>();
            services.AddScoped<IUploadSessionRepository, UploadSessionRepository>();
            services.AddScoped<ILibraryWorkspaceActionLogic, LibraryWorkspaceActionLogic>();
            services.AddScoped<IPlayoutRepository, PlayoutRepository>();
            services.AddScoped<IActionLoggerRepository, ActionLoggerRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserLogic, UserLogic>();
            services.AddScoped<IPriorApprovalRepository, PriorApprovalRepository>();
            services.AddScoped<IPriorApprovalLogic, PriorApprovalLogic>();
            services.AddScoped<IAlbumAPICallsRepository, AlbumAPICallsRepository>();
            services.AddScoped<IAlbumAPIResultsRepository, AlbumAPIResultsRepository>();
            services.AddScoped<IAlbumSyncSessionRepository, AlbumSyncSessionRepository>();
            services.AddScoped<ILogSyncTimeRepository, LogSyncTimeRepository>();
            services.AddScoped<IUploaderLogic, UploaderLogic>();
            services.AddScoped<IElasticLogic, ElasticLogic>();
            services.AddScoped<ILogLogic, LogLogic>();            
            services.AddSingleton<IActionLoggerLogic, ActionLoggerLogic>();
            services.AddScoped<ITunecodeIsrcRepository, TunecodeIsrcRepository>();
            services.AddScoped<IChartRepository, ChartRepository>();
            services.AddScoped<IPRSMLMasterRepository, PRSMLMasterRepository>();
            services.AddSingleton<ITrackXMLMapper, TrackXmlMapper>();
            services.AddSingleton<IPreSignedUrlProvider, PreSignedUrlProvider>();
            services.AddScoped<PrsSearch.PrsAuth.IAuthentication, PrsSearch.PrsAuth.Authentication>();            
        }
    }
}


