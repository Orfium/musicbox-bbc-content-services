using dotenv.net;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace MusicManager.SyncService
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder ConfigureEnvironmentVariablesFromDotEnvFile(this IHostBuilder hostBuilder)
        {
            const string envFileName = ".env";

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { Path.Combine(baseDirectory, envFileName) }));
            return hostBuilder;
        }
    }
}
