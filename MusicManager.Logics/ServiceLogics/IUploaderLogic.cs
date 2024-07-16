using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IUploaderLogic
    {       
        Task CreateUploadTracksOnDatahubAndML(List<UploadTrackT1> uploadTrackT1,string orgId,org_user org_User);
        Task UpdateDatahubTracksByUploadTracks(List<upload_track> uploadTracks);
        Task<MLTrackDocument> CreateMasterTrackAndAlbumFromUploads(string orgId, Guid dhTrackId,EditTrackMetadata editTrackMetadata, 
            EditAlbumMetadata editAlbumMetadata,
            org_user orgUser,
            upload_track uploadTrack,
            bool indexAlbum, enUploadRecType enUploadRecType,bool isLiveTrack = false );

        Task<MLTrackDocument> UpdateMasterTrackAndAlbumFromUploads(Guid dhTrackId,
             Track track,
             EditTrackMetadata editTrackMetadata,
             EditAlbumMetadata editAlbumMetadata,
             bool isAlbumUpdate,
             long updatedEpochTime, string prsTunecode);
        Task UpdateAlbumIndex(EditAlbumMetadata editAlbumMetadata, Guid dhProdId);
        Task ChangeTrackAlbum(Guid versionId, Track track,Guid uploadId);
        Task UploadArtwork(string key,byte[] content,Guid mlAlbumId);
        Task<int> DeleteTrackBulk(DeleteTrackPayload trackEditDeletePayload);
        Product CreateProductFromEditAlbumMetadata(EditAlbumMetadata editAlbumMetadata);
        Task<UploadObject> ProcessUploaderFiles(TrackXMLPayload trackXMLPayload);
        Task<MLTrackDocument> CreateNewTrack(TrackCreatePayload trackCreatePayload);
        Task<long> NextUploadSessionId();
    }
}
