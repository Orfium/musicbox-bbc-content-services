using MusicManager.Core.ExternalApiModels;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IMLMasterTrackRepository : IGenericRepository<ml_master_track>
    {       
        Task<IEnumerable<MLMasterTrack>> SearchAllFor_CTag_EMI();
        Task<int> GetMasterTracksCountByAlbumId(Guid albumId, string orgId);
        Task<IEnumerable<ml_master_track>> GetMasterTracksByAlbumId(Guid albumId);
        Task<DHTrackEdit> GetTrackForEdit(TrackEditDeletePayload trackEditDeletePayload, string enWorkspaceType, ml_master_track ml_Master_Track, ml_master_album mlMasterAlbum, track_org track_Org, album_org album_Org);       
        UpdateDHTrackPayload UpdateEditedTrackObjects(UpdateDHTrackPayload updateDHTrackPayload);
        Task<int> UpdateEditeTrackAndAlbumMetadata(ml_master_track ml_Master_Track);
        Task CreateTempElasticIndex(MLTrackDocument mLTrackDocument);
        Task UpdateTrackByMLTrackId(Guid mlTrackid, Guid dhAlbumId, DHAlbum dhAlbum);        
        Task<ExtTrack> GetElasticTrackById(Guid trackId);
        Task<int> RemoveTrack(DeleteTrackPayload trackEditDeletePayload, enWorkspaceType enWorkspaceType);
        Task<ml_master_track> GetMaterTrackById(Guid trackId);
        Task<ml_master_track> GetMaterTrackByTrackOrgIdOrDhTrackId(track_org track_Org);
        //Task<MLMasterTrack> CheckMLMasterTrackFroPRS(string trackId);
        Task<IEnumerable<ml_master_track>> GetMasterTrackListForSetLive(enWorkspaceLib type,Guid refId, long? lastSyncApiResultId, IEnumerable<library_org> library_Orgs,int limit, string orgId,int pageNo);
        Task<IEnumerable<ml_master_album>> GetMasterAlbumListForSetLive(enWorkspaceLib type, Guid refId, long? lastSyncApiResultId, IEnumerable<library_org> library_Orgs, int limit);
        Task<IEnumerable<ml_master_album>> GetMissingMasterAlbumListForSetLive(enWorkspaceLib type, Guid refId, long lastSyncApiResultId, IEnumerable<library_org> library_Orgs, int limit);
        Task<ml_master_track> GetMaterTrackByMlId(Guid trackOrgId, string orgId);       
        Task<int> GetMasterTracksCountByWorkspaceId(Guid wsId);
        Task<int> GetMasterTracksCountByLibraryId(Guid libId);
        Task<IEnumerable<ml_master_track>> GetMasterTrackListByIdList(Guid[] ids);
        Task<int> GetTrackUpdatedCountCount(DateTime date);
    }
}
