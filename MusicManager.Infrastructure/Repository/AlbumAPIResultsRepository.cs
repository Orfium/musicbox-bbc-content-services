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
    public class AlbumAPIResultsRepository : GenericRepository<log_album_api_results>, IAlbumAPIResultsRepository
    {
        public AlbumAPIResultsRepository(MLContext context,
            IOptions<AppSettings> appSettings, ILogger<AlbumAPIResultsRepository> logger) : base(context)
        {
            Context = context;
            _AppSettings = appSettings;
            _Logger = logger;
        }

        public MLContext Context { get; }
        public IOptions<AppSettings> _AppSettings { get; }
        public ILogger<AlbumAPIResultsRepository> _Logger { get; }

        public async Task<int> BulkInsert(List<log_album_api_results> logAlbumApiResults)
        {
            int count = 0;
            try
            {
                foreach (var item in logAlbumApiResults)
                {
                    count += await Insert(item);                   
                }               
                return count;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public async Task<int> Insert(log_album_api_results log_Album_Api_Results)
        {
            try
            {
                using (var c = new NpgsqlConnection(_AppSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"INSERT INTO log.log_album_api_results(
	api_call_id, album_id, workspace_id, version_id, received, deleted, metadata, session_id, date_created, date_modified,created_by)
	VALUES (@api_call_id, @album_id, @workspace_id, @version_id, @received, @deleted, CAST(@metadata AS json), @session_id, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,@created_by);", log_Album_Api_Results);
                }                
            }
            catch(Exception ex)
            {
                _Logger.LogError(ex, "Insert log_album_api_results | AlbumId: {AlbumId} , VersionId:{VersionId} , Metadata: {Metadata} , Module: {Module}",
                  log_Album_Api_Results.album_id, log_Album_Api_Results.version_id, JsonConvert.SerializeObject(log_Album_Api_Results.metadata),  "Album Sync");
                return 0;
            }
        }

        public async Task DeleteAllBySessionId(int sessionId)
        {
            try
            {
                using (var c = new NpgsqlConnection(_AppSettings.Value.NpgConnection))
                {
                    await c.ExecuteAsync(string.Format(@"delete from log.log_album_api_results where session_id = {0}", sessionId));
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "DeleteAllBySessionId log_album_api_results");
            }
        }
    }
}
