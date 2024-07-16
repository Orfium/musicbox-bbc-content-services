using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MusicManager.UserSync
{
    public class Program
    {
        public static IConfiguration configuration;
        public static IOptions<AppSettings> appSettings;

        static void Main(string[] args)
        {
            InitializeService();
            //test();
            Task.Run(() => UserSync.SyncOrgUsers());
            Console.ReadLine();

        }
        static void InitializeService()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            configuration = builder.Build();

            var collection = new ServiceCollection();
            collection.Configure<AppSettings>(x => configuration.GetSection("AppIdentitySettings").Bind(x));

            var serviceProvider = collection.BuildServiceProvider();
            appSettings = serviceProvider.GetService<IOptions<AppSettings>>();

            new UserSync(appSettings);
        }

        static void test() {
            string str = Console.ReadLine();

            string patern = "\\s*(=>|,|\\s)\\s*";

            var rx = new Regex(@"\,|/+", RegexOptions.Compiled);

            string[] _list = rx.Split(str);

            foreach (var item in _list)
            {
                Console.WriteLine(item);
            }
            test();
        }
    }
}
