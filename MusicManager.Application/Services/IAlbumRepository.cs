using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IAlbumRepository : IGenericRepository<album>
    {
        Task<List<Album>> GetAlbumsForES(Album album);       
        Task CheckAndArchiveAlbums();
        Task<int> UpdateOrgData(album_org albumOrg);
        Task<int> UpdateChartInfoById(List<album_org> albumOrgs);
        Task<album_org> GetAlbumOrgByDhAlbumIdAndOrgId(Guid dhAlbumId, string orgId);
        Task<album_org> GetAlbumOrgById(Guid id);
        Task<ml_master_album> GetMlMasterAlbumById(Guid id);
        Task<int> GetOrgAlbumCountByWsOrgId(Guid wsOrgId);
        Task<int> GetMasterAlbumCountByWsId(Guid workspaceId);
        Task<IEnumerable<Guid>> GetDistinctAlbumIdFromTrackIds(List<Guid> trackIds);
        Task CheckAndUpdatePreviousAlbumOrgs(List<Guid> albumIds);
    }
}
