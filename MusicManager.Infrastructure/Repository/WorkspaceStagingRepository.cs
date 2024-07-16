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
    public class WorkspaceStagingRepository : GenericRepository<staging_workspace>, IWorkspaceStagingRepository
    {
        private readonly ILogger<WorkspaceStagingRepository> _logger;
        private readonly IOptions<AppSettings> _appSettings;

        public WorkspaceStagingRepository(MLContext context, ILogger<WorkspaceStagingRepository> logger,
            IOptions<AppSettings> appSettings) : base(context)
        {
            Context = context;
            _logger = logger;
            _appSettings = appSettings;
        }

        public MLContext Context { get; }       

        public async Task<int> BulkInsert(List<staging_workspace> stagingWorkspaces)
        {
            int successCount = 0;

            string sql = @"INSERT INTO staging.staging_workspace(
	workspace_id, workspace_name, track_count, deleted, date_created)
	VALUES (@workspace_id, @workspace_name, @track_count, @deleted, CURRENT_TIMESTAMP)
    on conflict (workspace_id) do update 
    set workspace_name=@workspace_name,track_count=@track_count, deleted=@deleted,date_created=CURRENT_TIMESTAMP;";

            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    foreach (var item in stagingWorkspaces)
                    {
                        successCount += await c.ExecuteAsync(sql, item);
                    }
                }
                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkInsert");
                throw;
            }            
        }

        public int Truncate()
        {
            try
            {
                return Context.Database.ExecuteSqlRaw("truncate staging.staging_workspace");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Truncate staging_workspace");
                return -2;
            }            
        }
    }
}
