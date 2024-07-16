using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Core.ViewModules;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MusicManager.SyncService
{
    public class Worker : BackgroundService
    {
        private static string _appVersion = "1.45.55";

        private readonly ILogger<Worker> _logger;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly MLService _mLService;
        private int playoutS3CleanupHour = 0;

        public Worker(ILogger<Worker> logger,
            IServiceScopeFactory factory,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
            _mLService = factory.CreateScope().ServiceProvider.GetRequiredService<MLService>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service {id} | Version {version}", _appSettings.Value.ServiceId, _appVersion);

            while (!stoppingToken.IsCancellationRequested)
            {
                //_logger.LogInformation("Service {id} running at: {time}", _appSettings.Value.ServiceId,DateTimeOffset.Now);

                switch (_appSettings.Value.ServiceId)
                {
                    case 1: //--- WS Lib Sync Service
                        await _mLService.SyncWS();
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        await _mLService.SyncLibrary();
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        await _mLService.SyncNewLibrariesAfterSetLive();
                        await Task.Delay(TimeSpan.FromMinutes(25), stoppingToken);
                        break;

                    case 2: //--- Master Sync Service
                        await _mLService.DownloadMasterDHTracks();
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        break;
                    case 3: //--- Track Sync Service
                        await _mLService.DownloadDHTracks();
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                        break;
                    case 4: //--- Uploader Service
                        await _mLService.ProcessUploadedTracks();
                        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
                        break;
                    case 5:  //--- Playout Service
                        await _mLService.PublishPlayouts();
                        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

                        if (playoutS3CleanupHour < DateTime.Now.Hour)
                        {
                            await _mLService.PlayoutS3Cleanup();
                            playoutS3CleanupHour = DateTime.Now.Hour;
                        }
                        break;
                    case 6: //--- Nighttime/PRS Service
                        await _mLService.TakedownByValidTo();
                        await _mLService.SearchableByValidFrom();
                        await _mLService.PreReleaseByValidFrom();

                        if (_appSettings.Value.IsLocal == false)
                            await _mLService.PRSIndex(false);

                        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                        break;
                    case 7: //--- Copy live tracks
                        await _mLService.LiveTrackCopyToMLDatabase();                      

                        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                        break;
                }                
            }
        }
    }
}
