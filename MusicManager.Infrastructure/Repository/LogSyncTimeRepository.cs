using Dapper;
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
    public class LogSyncTimeRepository : GenericRepository<log_sync_time>, ILogSyncTimeRepository
    {
        public MLContext _context { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public LogSyncTimeRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
        }

        public async Task<long> Save(log_sync_time log_Sync_Time)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<long>(@"INSERT INTO log.log_sync_time(
                workspace_id, org_id, track_download_start_time,service_id)
	            VALUES (@workspace_id, @org_id, @track_download_start_time,@service_id) returning id;", log_Sync_Time);
            }
        }

        public async Task<int> UpdateLogSyncTime(log_sync_time log_Sync_Time)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"UPDATE log.log_sync_time
	SET  track_download_end_time=@track_download_end_time, track_download_time=@track_download_time, 
	album_download_start_time=@album_download_start_time, album_download_end_time=@album_download_end_time, 
	album_download_time=@album_download_time, sync_start_time=@sync_start_time, sync_end_time=@sync_end_time, 
	sync_time=@sync_time, track_index_start_time=@track_index_start_time, track_index_end_time=@track_index_end_time, 
	track_index_time=@track_index_time, album_index_start_time=@album_index_start_time, album_index_end_time=@album_index_end_time, 
	album_index_time=@album_index_time, total_time=@total_time, status=@status, download_tracks_count=@download_tracks_count, 
	download_albums_count=@download_albums_count, sync_tracks_count=@sync_tracks_count, sync_albums_count=@sync_albums_count, 
	index_tracks_count=@index_tracks_count, index_albums_count=@index_albums_count,completed_time=@completed_time
	WHERE id=@id;", log_Sync_Time);
                }
            }
            catch (Exception)
            {
                throw;
            }            
        }

        public async Task<bool> CheckServiceStatus(Guid workspaceId)
        {
            throw new NotImplementedException();
        }
    }
}
