using Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using MusicManager.Infrastructure;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MusicManager.SyncApp
{
    class Program
    {

        private static string _appVersion = " 1.44.44";
        
        static async Task Main(string[] args)
        {
          
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);
            var configuration = builder.Build();

            IHost host = ConfigureService(configuration);

            var svc = ActivatorUtilities.CreateInstance<MLService>(host.Services);           

            Console.WriteLine("Select the service 6");
            Console.WriteLine("-1 - Create Album index");
            Console.WriteLine("0 - Create Track index");
            Console.WriteLine("1 - Sync WS");           
            Console.WriteLine("3 - Download DH Tracks");
            Console.WriteLine("4 - Sync Tracks");
            Console.WriteLine("5 - Elastic Index");
            Console.WriteLine("6 - Sync Master Workspace");   
            Console.WriteLine("9 - Process uploaded tracks");
            Console.WriteLine("10 - PRS Index");           
            Console.WriteLine("12 - Message Queue Consumer");
            Console.WriteLine("14 - Tunecode ISRC Import");

            string _serviceId = configuration.GetSection("AppSettings:ServiceId").Value; 
            string path = configuration.GetSection("AppSettings:TunecodePath").Value;

            switch (_serviceId)
            {               
                case "1":
                    await svc.SyncWS(true);
                    break;                
                case "3":
                    await svc.DownloadDHTracks();                                      
                    break;
                case "4":
                    await Task.Run(() => svc.IndexedCtags());
                    break;             
                case "6":   
                    await svc.DownloadMasterDHTracks();
                    break;
                case "7":
                    await Task.Run(() => svc.SyncMasterCharts());
                    break;
                case "9":
                    await svc.ProcessUploadedTracks();
                    break;
                case "10":                   
                    await svc.DailyNightTimeService();
                    break;
                case "11":
                    await svc.FixCtagRules();
                    break;
                case "12":
                    await svc.ReadPlayoutResponse();
                    break;
                case "13":
                    await svc.PublishPlayouts();
                    break;                
                case "14":
                    await Task.Run(() => svc.TuneCodeISRCImport(path));
                    break;
                case "15":
                    await Task.Run(() => svc.UpdateAlbumChartIndicator( path));
                    break;
                case "16":
                    await Task.Run(() => svc.UpdateTrackChartIndicator(path));
                    break;
                case "17":
                    await svc.UploadAsset();
                    break;

            }            
        }

        static IHost ConfigureService(IConfigurationRoot configuration)
        {
            var logsEnvironment = configuration.GetSection("AppSettings:AppEnvironment").Value ?? "dev";
            var serviceId = configuration.GetSection("AppSettings:ServiceId").Value;

            Log.Logger = new LoggerConfiguration()            
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Conditional(con=> logsEnvironment=="dev",wt=>wt.Console())
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(node: new Uri(configuration.GetSection("AppSettings:elasticsearch:url").Value))
                {
                    IndexFormat = $"medialibrary-{logsEnvironment}-app-log-{{0:yyyy-MM}}",
                    AutoRegisterTemplate = true,
                    NumberOfShards = 2,
                    NumberOfReplicas = 1
                })                
                .CreateLogger();
            

            Log.Logger.Information($"App starting - Service Id ({serviceId}) - V {_appVersion}");

            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddMemoryCache();
                    services.AddDbContextPool<MLContext>(_builder =>
                    _builder.UseNpgsql(configuration.GetSection("AppSettings:NpgConnection").Value));
                    services.AddElasticsearch(configuration);                    
                    services.AddAwsServices();
                    services.Configure<AppSettings>(x => configuration.GetSection("AppSettings").Bind(x));                   
                    services.AddInfrastructure();
                    services.AddHttpClient();
                })
                .UseSerilog()
                .Build();
        }

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();
        }            
    }
}
