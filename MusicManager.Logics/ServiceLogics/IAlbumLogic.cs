using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IAlbumLogic
    {
        Task<string> CreateAlbum(TrackUpdatePayload trackUpdatePayload);
        Task<string> UpdateAlbum(TrackUpdatePayload albumPayload);
        Task<int> AddTracksToAlbum(AddTrackToAlbumPayload albumPayload);       
        Task<string> DeleteAlbum(DeleteAlbumPayload albumPayload, enWorkspaceType wsType);
        Task<DHTrackEdit> CreateEditAlbum(AlbumkEditDeletePayload albumkEditDeletePayload);
        Task<List<upload_track>> ReorderAlbumTracks(TrackReorderPayload trackReorderPayload);
        Task UpdateAlbumOrgWhenEdit(EditAlbumMetadata editAlbumMetadata,org_user org_User,Guid? trackId);
        Task ClearEmptyUploadAlbums(IEnumerable<Guid> albumIds);
        Task UpdateRecordLabelOfAllAlbumTracks(Guid albumId,string recordLabel, Guid updatedTrackId, string orgId, EditAlbumMetadata editAlbumMetadata);
    }
}
