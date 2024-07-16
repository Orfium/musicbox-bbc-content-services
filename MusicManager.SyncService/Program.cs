using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using MusicManager.Core.Models;
using Microsoft.EntityFrameworkCore;
using Elasticsearch;
using MusicManager.Infrastructure;
using MusicManager.Core.ViewModules;
using Microsoft.Extensions.Configuration;

namespace MusicManager.SyncService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureEnvironmentVariablesFromDotEnvFile()
            .UseSerilog((context, configuration) =>
            {
                var logsEnvironment = context.Configuration.GetSection("AppSettings:AppEnvironment").Value ?? "dev";

                configuration.Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(node: new Uri(context.Configuration.GetSection("AppSettings:elasticsearch:url").Value))
                {
                    IndexFormat = $"medialibrary-{logsEnvironment}-app-log-{{0:yyyy-MM}}",
                    AutoRegisterTemplate = true,
                    NumberOfShards = 2,
                    NumberOfReplicas = 1
                })
                .Enrich.WithProperty("Envirenment", context.HostingEnvironment.EnvironmentName)
                .ReadFrom.Configuration(context.Configuration);
            })
            .ConfigureServices((hostContext, services) =>
            {
                IConfiguration configuration = hostContext.Configuration;
                services.AddMemoryCache();
                services.AddDbContextPool<MLContext>(_builder =>
                    _builder.UseNpgsql(configuration.GetSection("AppSettings:NpgConnection").Value));
                services.AddElasticsearch(configuration);
                services.AddAwsServices();
                services.Configure<AppSettings>(x => configuration.GetSection("AppSettings").Bind(x));
                services.AddInfrastructure();
                services.AddHttpClient();               
                services.AddScoped<MLService>();
                services.AddHostedService<Worker>();
            });
    }
}




