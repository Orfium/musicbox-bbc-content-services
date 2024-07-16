using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IUploadAlbumRepository : IGenericRepository<upload_album>
    {
        Task<upload_album> GetAlbum(upload_album upload_Album);
        Task<upload_album> CheckAlbumForUpload(upload_album upload_Album);
        Task<int> GetTrackCountOfCurrentSession(upload_album upload_Album);
        Task<upload_album> GetAlbumByCopySourceId(Guid sourceAlbumId);
        Task<upload_album> CreateAlbum(upload_album upload_album);        
        Task<string> UploadArtWork(Guid? id, string stream);
        Task<upload_album> GetAlbumById(Guid id);
        Task<upload_album> GetAlbumByProductId(Guid id);
        Task<int?> Save(upload_album upload_album);       
        Task<string> RemoveTrackFromAlbum(Guid trackId, enWorkspaceType enWorkspaceType);
        Task<string> RemoveUploadAlbum(upload_album upload_album);
        Task<int> UpdateAlbum(upload_album upload_album);
        Task<int> UpdateArtwork(upload_album upload_album);
        Task<int> UpdateAlbumByDHAlbumId(upload_album upload_album);
        Task<int> UpdateArtworkUploaded(upload_album upload_album);
        Task<bool> DeleteUploadAlbumAndDeleteFromDatahub(upload_album upload_album);
    }
}
