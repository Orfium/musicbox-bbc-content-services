using System.Threading.Tasks;

namespace MusicManager.SyncApp
{
    public interface IMLService
    {  
        Task SyncWS(bool init);
        Task SyncLibrary();
        Task DownloadDHTracks();    
        Task ProcessUploadedTracks();
        Task IndexedCtags();
        Task ClearCtags();
        Task UpdateAlbumChartIndicator(string path);
        Task UpdateTrackChartIndicator(string path);
        Task SyncMasterCharts();
        Task PRSIndex(bool charted);
        Task DailyNightTimeService();
        Task PublishPlayouts();
    }
}