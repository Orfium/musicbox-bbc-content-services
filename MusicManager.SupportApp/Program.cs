using Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using MusicManager.Infrastructure;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MusicManager.SupportApp
{
    class Program
    {
        public static IConfiguration configuration;
        public static IOptions<AppSettings> appSettings;
        public static ICTagsRepository CTagsRepository;
        static void Main(string[] args)
        {
            InitializeService();
            Task.Run(() => Service.AddPriorApprovals());
            Console.ReadLine();

        }
        static void InitializeService()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            configuration = builder.Build();

            var collection = new ServiceCollection(); 
            collection.AddDbContextPool<MLContext>(builder => builder.UseNpgsql(configuration.GetSection("AppIdentitySettings:ConnectionStrings:NpgConnection").Value));
            collection.Configure<AppSettings>(x => configuration.GetSection("AppIdentitySettings").Bind(x));
            collection.AddInfrastructure();
            var serviceProvider = collection.BuildServiceProvider();
            appSettings = serviceProvider.GetService<IOptions<AppSettings>>();
            CTagsRepository = serviceProvider.GetService<ICTagsRepository>();

            new Service(CTagsRepository, appSettings);
        }
    }
}
