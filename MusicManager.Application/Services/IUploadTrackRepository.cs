using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IUploadTrackRepository : IGenericRepository<upload_track>
    {
        Task<upload_track> Save(upload_track upload_Track);       
        Task<upload_track> GetTrack(upload_track upload_Track);
        Task<bool> UpdateTracksFromAlbumId(Guid trackId, Guid albumId,string dhAlbumId, EditTrackMetadata metadata);
        Task<bool> CheckAndUpdateWhenEdit(UpdateDHTrackPayload updateDHTrackPayload);
        Task<int> UpdateSyncStatus(Guid id, float? duration);
        Task<List<upload_track>> GetTracksFromAlbumId(Guid albumId);
        Task<int> UpdateByHDTrack(DHTrack dHTrack, DHAlbum dHAlbum, string artworkUrl, upload_track uploadTrack, upload_album uploadAlbum);
        Task<upload_track> GetUploadTrackById(Guid id);
        Task<upload_track> GetUploadTrackByUploadId(Guid id);
        Task<bool> RemoveAlbumFromTrack(Guid trackId, EditTrackMetadata metadata);
        Task<bool> UpdateTrackDhAlbumMetaData(upload_track track);
        Task<int> RemoveUploadTrackByUploadId(Guid id);
        Task<List<upload_track>> GetTracksByUploadStatus(string status);
        Task<List<upload_track>> GetTracksForAssetUpload(int retries = 2);
        Task<int> UpdateDHTrackId(upload_track upload_Track);
        Task<int> UpdateUploadTrack(upload_track upload_Track);
        Task<int> UpdateDHAlbumId(upload_track upload_Track);
        Task ReorderUploadTracks(List<upload_track> uploadTracks);
        Task<int> RemoveUploadTracksByUploadIds(Guid[] ids);
        Task<long> GetUploadSessionId();
        Task<int?> GetTrackCountByAlbumId(Guid mlAlbumId);
        Task<IEnumerable<Guid>> GetUniqueAlbumIdsByTrackIds(List<string> trackIds);
    }
}
