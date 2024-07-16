using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class LibraryStagingRepository : GenericRepository<staging_library>, ILibraryStagingRepository
    {
        private readonly ILogger<LibraryStagingRepository> _logger;
        private readonly IOptions<AppSettings> _appSettings;

        public LibraryStagingRepository(MLContext context, ILogger<LibraryStagingRepository> logger,
            IOptions<AppSettings> appSettings) : base(context)
        {
            Context = context;
            _logger = logger;
            _appSettings = appSettings;
        }

        public MLContext Context { get; }

        public async Task<int> BulkInsert(List<staging_library> stagingLibrary)
        {
            int successCount = 0;

            try
            {
                string sql = @"INSERT INTO staging.staging_library(
	library_id, library_name, workspace_id, track_count, deleted,date_created)
	VALUES (@library_id, @library_name, @workspace_id, @track_count, @deleted,@date_created)
    on conflict (library_id) do update 
    set library_name=@library_name, workspace_id=@workspace_id, track_count=@track_count, deleted=@deleted,date_created=@date_created;";

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    foreach (var item in stagingLibrary)
                    {
                        successCount += await c.ExecuteAsync(sql, item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkInsert Library");
            }
           
            return successCount;
        }

        public int Truncate()
        {
            try
            {
                return Context.Database.ExecuteSqlRaw("truncate staging.staging_library");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncWorkspaces");
                return 0;
            }          
        }
    }
}
