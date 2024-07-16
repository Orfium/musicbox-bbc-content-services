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
    public class PlayoutRepository : GenericRepository<playout_session>, IPlayoutRepository
    {
        public MLContext _context { get; }
        public ILogger<PlayoutRepository> _logger { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public PlayoutRepository(MLContext context, ILogger<PlayoutRepository> logger, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _logger = logger;
            _appSettings = appSettings;
        }

        public async Task<playout_session> SavePlayoutSession(playout_session playout)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                string sql = @"INSERT INTO playout.playout_session(
                            org_id, date_created, created_by, date_last_edited, session_date, station_id, last_status, track_count, build_id, request_json, publish_status,publish_attempts)
	                        VALUES(@org_id, CURRENT_TIMESTAMP, @created_by, CURRENT_TIMESTAMP, now(), @station_id, @last_status, @track_count, @build_id, CAST(@request_json as json),@publish_status,0) returning *; ";

                return await c.QuerySingleAsync<playout_session>(sql, playout);
            }
        }

        public async Task<int> SavePlayoutSessionTracks(playout_session_tracks tracks)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                string sql = @"INSERT INTO playout.playout_session_tracks(
                             date_created, created_by, date_last_edited, last_edited_by, session_id, status, type, track_id,track_type, title, isrc,performer,artwork_url,dh_track_id,label,album_title, duration,asset_status,xml_status)
	                        VALUES(CURRENT_TIMESTAMP, @created_by, CURRENT_TIMESTAMP, @last_edited_by, @session_id, @status, @type, @track_id, @track_type, @title, @isrc, @performer, @artwork_url,@dh_track_id,@label,@album_title, @duration,@asset_status,@xml_status); ";

                return await c.ExecuteAsync(sql, tracks);
            }
        }

        public async Task SavePlayoutResponse(playout_response response)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                   await c.ExecuteAsync(@"INSERT INTO playout.playout_response(
                             build_id, request_id, response_json, response_time, status)
                            VALUES(@build_id, @request_id, CAST(@response_json as json), @response_time, @status);", response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SavePlayoutResponse | Buildid: {Buildid} , Module: {Module}", response.build_id, "Playout");
            }
        }

        public async Task<radio_stations> GetRadioStationById(Guid id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<radio_stations>(@"select * from playout.radio_stations rs where rs.id = @id;", new radio_stations() { id = id });
            }
        }

        public async Task<int> DeletePlayoutTracks(long id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"delete from playout.playout_session_tracks where id = @id;", new playout_session_tracks() { id = id });
            }
        }

        public async Task<playout_session> GetPlayoutSessionById(int id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<playout_session>(@"select * from playout.playout_session ps where ps.id = @id;", new playout_session() { id = id });
            }
        }

        public async Task<int> UpdatePlayoutSessionStatus(playout_session playout_Session)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session set last_status=@last_status where id = @id;", playout_Session);
            }
        }

        public async Task<playout_session> GetPlayoutSessionByBuildId(Guid build_id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<playout_session>(@"select * from playout.playout_session ps where ps.build_id = @build_id;", new playout_session() { build_id = build_id });
            }
        }

        public async Task<IEnumerable<playout_session_tracks>> GetPlayoutTracksBySessionId(int sessionId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<playout_session_tracks>(@"SELECT *  FROM playout.playout_session_tracks pt                                                         
                                                          where pt.session_id = @session_id;", new playout_session_tracks() { session_id = sessionId });               
            }
        }

        public async Task<playout_session_tracks> GetPlayoutTrackById(int id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<playout_session_tracks>(@"select * from playout.playout_session_tracks pst where pst.id = @id;", new playout_session_tracks() { id = id });
            }
        }
        public async Task<int> UpdateTrackTypeById(int id, string track_type, enPlayoutTrackStatus enPlayoutTrackStatus)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update  playout.playout_session_tracks set track_type=@track_type,status=@status where id = @id;", new playout_session_tracks() { id = id, track_type = track_type,status= (int)enPlayoutTrackStatus });
            }
        }

        public async Task<int> UpdatePlayoutSessionById(playout_session playout_session)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session set request_json=CAST(@request_json as json), build_id=@build_id, station_id=@station_id,last_status=@last_status,date_last_edited=CURRENT_TIMESTAMP where id = @id;", playout_session);
            }
        }

        public async Task<IEnumerable<playout_session>> GetPendingPlayoutsessions()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<playout_session>(@"select ps.* from playout.playout_session ps
                left join playout.playout_response pr on ps.build_id = pr.build_id 
                where ps.last_status = 2 and pr.build_id is null");                
            }
        }

        public async Task<playout_response> GetTheLastResponse(Guid build_id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<playout_response>(@"SELECT * FROM playout.playout_response pr 
where pr.build_id = @build_id
order by pr.response_id desc limit 1", new playout_response() { build_id = build_id });
            }
        }

        public async Task<int> UpdatePlayoutSessionPublishStatus(playout_session playout_session)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session set publish_status=@publish_status,date_last_edited=CURRENT_TIMESTAMP where id = @id;", playout_session);
            }
        }

        public async Task<IEnumerable<playout_session>> GetPlayoutSessionsForPublish()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<playout_session>(@"select * from playout.playout_session ps                
                where ps.publish_status = 1 or ps.publish_status = 2;");
            }
        }

        public async Task<int> UpdateSigniantRefId(playout_session playout_session)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session set signiant_ref_id=@signiant_ref_id,
                date_last_edited=CURRENT_TIMESTAMP where id = @id;", playout_session);
            }
        }

        public async Task<int> UpdatePublishStatus(playout_session playoutSession)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("update playout.playout_session set publish_status=@publish_status,date_last_edited = CURRENT_TIMESTAMP where ");

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    if (playoutSession.id > 0)
                    {
                        stringBuilder.Append(" id = @id;");
                        return await c.ExecuteAsync(stringBuilder.ToString(), playoutSession);
                    }
                    else if (playoutSession.build_id != null)
                    {
                        stringBuilder.Append(" build_id = @build_id;");
                        return await c.ExecuteAsync(stringBuilder.ToString(), playoutSession);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }            
            return 0;
        }

        public async Task<int> UpdateTrackXmlStatus(playout_session_tracks playoutSessionTracks)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session_tracks set xml_status=@xml_status,
                date_last_edited=CURRENT_TIMESTAMP where id = @id;", playoutSessionTracks);
            }
        }

        public async Task<int> UpdateTrackAssetStatus(playout_session_tracks playoutSessionTracks)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session_tracks set asset_status=@asset_status,
                date_last_edited=CURRENT_TIMESTAMP where id = @id;", playoutSessionTracks);
            }
        }

        public async Task<int> UpdateTrackStatus(playout_session_tracks playoutSessionTracks)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session_tracks set status=@status,
                date_last_edited=CURRENT_TIMESTAMP where id = @id;", playoutSessionTracks);
            }
        }

        public async Task<IEnumerable<playout_session>> GetS3CleanupSessions()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<playout_session>(@"select * from playout.playout_session ps                
                where ps.s3_cleanup = @s3_cleanup and ps.session_date<@session_date and ps.last_status=@last_status;",
                new playout_session() { session_date = DateTime.Now.AddDays(-7), last_status = 4, s3_cleanup = false });
            }
        }

        public async Task<int> UpdateS3Cleanup(int id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session set s3_cleanup = @s3_cleanup
                 where id = @id;", new { id = id, s3_cleanup = true });
            }
        }

        public async Task<int> UpdateAttempts(playout_session playoutSession)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update playout.playout_session set publish_attempts=@publish_attempts,
                date_last_edited=CURRENT_TIMESTAMP where id = @id;", playoutSession);
            }
        }

        public async Task<IEnumerable<playout_session_tracks>> GetAssetAvilablePlayoutTracksBySessionId(int sessionId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<playout_session_tracks>(@"SELECT *  FROM playout.playout_session_tracks pt                                                         
                                                          where pt.session_id = @session_id and pt.asset_status > 0", new playout_session_tracks() { session_id = sessionId });
            }
        }

        public async Task<int> UpdatePublishStartTime(int id)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync("update playout.playout_session set publish_start_datetime=CURRENT_TIMESTAMP where id = @id;",
                        new { id = id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePublishStartTime | Playout Id: {ID}, Module: {Module}", id, "Playout");
                return 0;
            }                     
        }

        public async Task<int> RestartPlayout(int playoutId)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"update playout.playout_session_tracks set xml_status=1, asset_status=1
                    where session_id = @session_id; update playout.playout_session set publish_status=1,publish_attempts=0 where id=@session_id;",
                        new { session_id = playoutId });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
