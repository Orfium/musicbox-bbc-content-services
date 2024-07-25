using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MusicManager.InitDB
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().RunAsync().GetAwaiter().GetResult();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });

        public class Worker : IHostedService
        {
            private readonly IConfiguration _configuration;
            private readonly IHostApplicationLifetime _appLifetime;

            public Worker(IConfiguration configuration, IHostApplicationLifetime appLifetime)
            {
                _configuration = configuration;
                _appLifetime = appLifetime;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                string connectionString = _configuration.GetSection("AppSettings:NpgConnection").Value;

                // Get the current assembly
                var assembly = Assembly.GetExecutingAssembly();

                // Get the resource stream
                using var stream = assembly.GetManifestResourceStream("MusicManager.InitDB.db_schema.sql");

                // Read the SQL script from the resource stream
                using var reader = new StreamReader(stream);
                string sql = await reader.ReadToEndAsync(cancellationToken);

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    using var command = new NpgsqlCommand(sql, connection);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                Console.WriteLine("SQL script executed successfully.");
                // Stop the application after the work is done
                _appLifetime.StopApplication();
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}