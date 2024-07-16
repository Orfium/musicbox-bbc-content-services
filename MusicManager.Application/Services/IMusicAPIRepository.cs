using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IMusicAPIRepository
    {
        Task<DHTrack> UpdateTrack(string trackId, dynamic mA_Track);
        Task<DHAlbum> UpdateAlbum(string albumId, dynamic albumData);
        Task<HttpWebResponse> CreateTrack(string workspaceId, dynamic DHTrack);
        //Task<Guid?> CreateTrackAndGetId(string workspaceId, dynamic dhTrack);
        Task<DHAlbum> CreateAlbum(string workspaceId, DHAlbum dHAlbum);
        //Task<Guid?> CreateAlbumAndGetId(string workspaceId, dynamic dhAlbum);
        Task<DHTrack> CreateUploadTrack(string workspaceId , upload_track track, Guid? albumId, org_user orgUser);
        Task<DHTrack> CreateDHTrack(string workspaceId, DHTrack dHTrack);       
        Task<DHAlbum> PrepareAndCreateAlbum(string workspaceId, upload_album upload_Album, org_user orgUser);
        Task<HttpWebResponse> GetTrackByUniqueId(string workspaceId, string uniqueId);
        Task<HttpStatusCode> UploadTrack(string trackId, byte[] fileStream);
        Task<HttpStatusCode> UploadArtwork(string albumId, byte[] fileStream);
        Task<HttpWebResponse> SendImportBegin(List<MA_BulkUploadPayload> bulkUploadPayloads);
        Task<HttpWebResponse> CheckImportStatus(List<string> trackIds);
        Task<HttpWebResponse> DHAssetCopy(Guid trackId,string assetURL);
        Task<HttpWebResponse> DeleteTrack(string trackId);
        Task<HttpWebResponse> DeleteAlbum(string albumId);
        Task<string> GetAlbumArtwork(Guid albumId);
        Task<DHAlbum> GetAlbumById(Guid albumId);
        Task<DHTrack> GetTrackById(string trackId);

        Task CopyArtwork(Guid albumId,string artworkUrl);
    }
}
