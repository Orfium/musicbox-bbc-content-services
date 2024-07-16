using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class TrackSyncSessionRepository : GenericRepository<log_track_sync_session>, ITrackSyncSessionRepository
    {
        public MLContext _context { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public TrackSyncSessionRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
        }

        public MLContext Context { get; }         

        public async Task<log_track_sync_session> SaveTrackSyncSession(log_track_sync_session logTrackSyncSession)
        {          
            string sql = @"INSERT INTO log.log_track_sync_session(
	        session_start, workspace_id, synced_tracks_count, download_tracks_count, status, page_token)
	        VALUES (CURRENT_TIMESTAMP, @workspace_id, @synced_tracks_count, @download_tracks_count, @status, CAST(@page_token AS json)) returning *;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleAsync<log_track_sync_session>(sql, logTrackSyncSession);
            }
        }

        public async Task<int> UpdateTrackSyncSession(log_track_sync_session logTrackSyncSession)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update log.log_track_sync_session set status=@status, download_tracks_count=@download_tracks_count,
                synced_tracks_count=@synced_tracks_count,session_end=CURRENT_TIMESTAMP,
                download_time=@download_time 
                where session_id=@session_id;", logTrackSyncSession);
            }
        }
    }
}
