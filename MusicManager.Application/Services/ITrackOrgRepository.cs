using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ITrackOrgRepository : IGenericRepository<track_org>
    {
        Task<int> ArchiveTrackAlbum(SyncActionPayload syncActionPayload);
        Task<int> InsertUpdateTrackOrg(List<track_org> trackOrgs);
        Task<int> InsertUpdateAlbumOrg(List<album_org> albumOrgs);
        Task<int> UpdateTrackOrg(track_org track_Org);
        Task<int> Restrict(SyncActionPayload syncActionPayload);
        Task<track_org> GetTrackOrgByDhTrackIdAndOrgId(Guid dhTrackId, string orgId);
        Task<track_org> GetById(Guid trackOrgId);
        Task<int> UpdateOrgData(track_org trackOrg);
        Task<int> UpdateChangeLog(TrackChangeLog trackChangeLog, Guid? trackOrgId, Guid? dhTrackId, string orgId);
        Task<int> UpdateTrackOrgByOriginalTrackId(track_org track_Org);
        Task<int> GetTrackOrgCountByWsOrgId(Guid wsOrgId);
        Task<int> UpdateChartInfo(TrackChartInfo trackChartInfo, Guid mlTrackId);
        Task<int> UpdateChartInfoBulk(List<track_org> trackOrgs);
        Task<int> UpdateChartArtist(bool status, Guid trackId);
        Task<int> UpdateChartAlbumArtist(bool status, Guid albumId);
        Task<int> UpdateTrackContentAlert(bool status, ContentAlert contentAlert, int userid);
        Task<int> UpdateAlbumContentAlert(bool status, ContentAlert contentAlert, int userid);
        Task<int> UpdateResolveAlbumContentAlert(bool status, ResolveContentAlert contentAlert, int userid);
        Task<int> UpdateResolveTrackContentAlert(bool status, ResolveContentAlert contentAlert, int userid);
        Task<int> UpdateTrackOrgByAlbumId(Guid albumId,Guid? trackId);
        Task<int> GetNewTracksCount(DateTime date,string orgId);
    }
}
