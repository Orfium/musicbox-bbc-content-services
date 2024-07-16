using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace MusicManager.Infrastructure.Repository
{
    public class TrackAPIResultsRepository : GenericRepository<log_track_api_results>, ITrackAPIResultsRepository
    {
        private readonly ILogger<TrackAPIResultsRepository> _logger;
        public MLContext _context { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public TrackAPIResultsRepository(MLContext context,
            IOptions<AppSettings> appSettings, ILogger<TrackAPIResultsRepository> logger) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
            _logger = logger;
        }
        

        public async Task<int> BulkInsert(List<log_track_api_results> log_Track_Api_Results)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<MLContext>();
                optionsBuilder.UseNpgsql(_context.Database.GetDbConnection().ConnectionString);

                using (MLContext _context = new MLContext(optionsBuilder.Options))
                {
                    _context.log_track_api_results.AddRange(log_Track_Api_Results);
                    return await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Track BulkInsert | Module: {Module}", "Track Sync");
                throw;
            }
        }

        public async Task DeleteAllBySessionId(int sessionId)
        {
            try
            {
                using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    await c.ExecuteAsync(string.Format(@"delete from log.log_track_api_results where session_id = {0}", sessionId));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> Insert(log_track_api_results log_Track_Api_Results)
        {
            try
            {               
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"INSERT INTO log.log_track_api_results(
	            api_call_id, track_id, workspace_id, version_id, received, deleted, metadata, session_id, date_created, created_by)
	            VALUES ( @api_call_id, @track_id, @workspace_id, @version_id, @received, @deleted, CAST(@metadata AS json), @session_id, @date_created, @created_by);", log_Track_Api_Results);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Insert log_track_api_results | TrackId: {TrackId}, Received : {Received} , Metadata: {Metadata} , Module: {Module}",
                  log_Track_Api_Results.track_id, log_Track_Api_Results.received, JsonConvert.SerializeObject(log_Track_Api_Results.metadata), "Track Sync");
                return 0;
            }           
        }
    }
}

