using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IChartRepository : IGenericRepository<chart_master_tracks>
    {
        Task<int> InsertUpdateMasterTracksBulk(List<chart_master_tracks> chartMasterTracks);
        Task<int> InsertUpdateMasterAlbumBulk(List<chart_master_albums> chartMasterAlbums);
        Task<MasterTrackChartResponse> GetAllTrackMasterTracks(chart_sync_summary trackChartSyncSummary, string chartTypeId);
        Task<MasterAlbumChartResponse> GetAllMasterAlbums(chart_sync_summary trackChartSyncSummary, string chartTypeId);
        Task<chart_sync_summary> GetLastChartSync(Guid chartTypeId,string type);
        Task<chart_sync_summary> InsertChartSyncSummary(chart_sync_summary chartSyncSummary);
        Task<IEnumerable<string>> GetDistinctTrackArtists();
        Task<IEnumerable<string>> GetDistinctAlbumArtists();
    }
}
