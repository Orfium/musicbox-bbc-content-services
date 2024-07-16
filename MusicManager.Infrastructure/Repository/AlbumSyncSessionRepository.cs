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
    public class AlbumSyncSessionRepository : GenericRepository<log_album_sync_session>, IAlbumSyncSessionRepository
    {
        private readonly IOptions<AppSettings> _appSettings;
        public MLContext _context { get; }
        public AlbumSyncSessionRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
        }

        public async Task<log_album_sync_session> SaveAlbumSyncSession(log_album_sync_session log_Album_Sync_Session)
        {

            string sql = @"INSERT INTO log.log_album_sync_session(
	        session_start, workspace_id, synced_tracks_count, download_tracks_count, status, page_token)
	        VALUES (CURRENT_TIMESTAMP, @workspace_id, @synced_tracks_count, @download_tracks_count, @status, CAST(@page_token AS json)) returning *;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleAsync<log_album_sync_session>(sql, log_Album_Sync_Session);
            }

        }

        public async Task<int> UpdateAlbumSyncSession(log_album_sync_session log_Album_Sync_Session)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update log.log_album_sync_session set status=@status, download_tracks_count=@download_tracks_count,
                synced_tracks_count=@synced_tracks_count,session_end=CURRENT_TIMESTAMP,
                download_time=@download_time 
                where session_id=@session_id;", log_Album_Sync_Session);
            }
        }
    }
}
