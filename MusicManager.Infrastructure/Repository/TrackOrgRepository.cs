using Dapper;
using Microsoft.EntityFrameworkCore;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Enum;
using Npgsql;
using System;
using System.Text;
using System.Threading.Tasks;
using MusicManager.Core.Payload;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Nest;
using MusicManager.Core.ViewModules;
using Microsoft.Extensions.Options;
using System.Linq;
using MusicManager.Application;
using Newtonsoft.Json;
using MusicManager.Logics.ServiceLogics;

namespace MusicManager.Infrastructure.Repository
{
    public class TrackOrgRepository : GenericRepository<track_org>, ITrackOrgRepository
    {
        public MLContext _context { get; }
        public ILogger<TrackOrgRepository> _Logger { get; }
        public IOptions<AppSettings> _appSettings { get; }
        public IElasticLogic _elasticLogic { get; }

        public TrackOrgRepository(MLContext context, ILogger<TrackOrgRepository> logger,
            IOptions<AppSettings> appSettings, IElasticLogic elasticLogic) : base(context)
        {
            _context = context;
            _Logger = logger;
            _appSettings = appSettings;
            _elasticLogic = elasticLogic;
        }

        public async Task<int> ArchiveTrackAlbum(SyncActionPayload syncActionPayload)
        {
            bool archive = false;

            if (syncActionPayload.action == enWorkspaceAction.ARCHIVE_TRACK.ToString())
            {
                archive = true;
            }
            int status = 0;
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {

                if (syncActionPayload.type == "track")
                {
                    //--- Update elastic index
                    _ = Task.Run(() => _elasticLogic.ArchiveTracks(syncActionPayload.ids.Select(Guid.Parse).ToArray(), true)).ConfigureAwait(false);
                }
                else
                {
                    //--- Update album elastic index
                    _ = Task.Run(() => _elasticLogic.RestoreTracks(syncActionPayload.ids.Select(Guid.Parse).ToArray(), true)).ConfigureAwait(false);
                }

                foreach (var item in syncActionPayload.ids)
                {
                    if (syncActionPayload.type == "track")
                    {

                        status = await c.ExecuteAsync(@"update track_org set archive=@archive, date_last_edited = CURRENT_TIMESTAMP where id=@id and org_id=@org_id",
                            new track_org()
                            {
                                id = Guid.Parse(item),
                                org_id = syncActionPayload.orgid,
                                archive = archive
                            });

                    }
                    else
                    {
                        status = await c.ExecuteAsync(@"update album_org set archive=@archive,last_edited_by=@last_edited_by,date_last_edited=CURRENT_TIMESTAMP
                                             where id=@id;
                                             update track_org set archive=@archive, date_last_edited = CURRENT_TIMESTAMP,last_edited_by=@last_edited_by 
                                             where album_id in (select original_album_id from album_org where id=@id) and org_id=@org_id",
                            new track_org()
                            {
                                id = Guid.Parse(item),
                                org_id = syncActionPayload.orgid,
                                archive = archive
                            });
                    }
                }
            }
            return status;
        }

        public async Task<int> InsertUpdateTrackOrg(List<track_org> trackOrgs)
        {
            int successCount = 0;

            try
            {
                DateTime dateTime = DateTime.Now;

                string sql = @"insert into public.track_org
(id, original_track_id, org_id, change_log, tags, date_created, date_last_edited, c_tags, album_id, source_deleted, restricted, archive, org_data, created_by, last_edited_by, ml_status, manually_deleted,org_workspace_id, api_result_id, chart_artist,clearance_track)
values(@id, @original_track_id, @org_id, CAST(@change_log AS json), CAST(@tags AS json), @date_created, @date_last_edited, CAST(@c_tags AS json), @album_id, @source_deleted, @restricted, @archive, CAST(@org_data AS json), @created_by, @last_edited_by, @ml_status, @manually_deleted,@org_workspace_id, @api_result_id, @chart_artist,@clearance_track) 
on conflict (original_track_id, org_id) do update 
set org_workspace_id=@org_workspace_id,org_id=@org_id,source_deleted=@source_deleted,last_edited_by=@last_edited_by, ml_status=@ml_status,date_last_edited=CURRENT_TIMESTAMP,api_result_id=@api_result_id,album_id=@album_id,chart_artist=@chart_artist";

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    foreach (var item in trackOrgs)
                    {
                        item.date_created = dateTime;
                        item.date_last_edited = dateTime;
                        successCount += await c.ExecuteAsync(sql, item);
                    }
                }

                if (successCount != trackOrgs.Count)
                {                   
                    _Logger.LogError("InsertUpdateTrackOrg > Count error - " + successCount + " - " + trackOrgs.Count);
                }
            }
            catch (Exception ex)
            {              
                _Logger.LogError(ex, "InsertUpdateTrackOrg");
            }

            return successCount;
        }

        public async Task<int> InsertUpdateAlbumOrg(List<album_org> albumOrgs)
        {
            int successCount = 0;

            string sql = @"INSERT INTO public.album_org(
id, original_album_id, org_id, change_log, tags, date_created, date_last_edited, c_tags, source_deleted, restricted, archive, org_data, created_by, last_edited_by, ml_status, manually_deleted, org_workspace_id,api_result_id,chart_artist)
VALUES (@id, @original_album_id, @org_id,  CAST(@change_log AS json), CAST(@tags AS json), CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CAST(@c_tags AS json), @source_deleted, @restricted, @archive, CAST(@org_data AS json), @created_by, @last_edited_by, @ml_status, @manually_deleted, @org_workspace_id,@api_result_id,@chart_artist)
on conflict (original_album_id, org_id) do update 
set source_deleted=@source_deleted,last_edited_by=@last_edited_by, ml_status=@ml_status,date_last_edited=CURRENT_TIMESTAMP,archive=@archive,chart_artist=@chart_artist";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                foreach (var item in albumOrgs)
                {
                    successCount += await c.ExecuteAsync(sql, item);
                }
            }
            return successCount;
        }

        public async Task<int> UpdateTrackOrg(track_org track_Org)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("update track_org set date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by ");

            if (track_Org.archive != null)
                stringBuilder.Append(",archive=@archive ");

            stringBuilder.Append(" where id=@id ");

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(stringBuilder.ToString(), track_Org);
            }
        }

        public async Task<int> UpdateTrackOrgByOriginalTrackId(track_org track_Org)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("update track_org set date_last_edited=CURRENT_TIMESTAMP ");

            if (!string.IsNullOrEmpty(track_Org.prs_details))
                stringBuilder.Append(",prs_details=CAST(@prs_details AS json) ");

            if (!string.IsNullOrEmpty(track_Org.c_tags))
                stringBuilder.Append(",c_tags=CAST(@c_tags AS json) ");

            stringBuilder.Append(" where original_track_id=@original_track_id ");

            if (!string.IsNullOrEmpty(track_Org.org_id))
                stringBuilder.Append(" and org_id=@org_id ");

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(stringBuilder.ToString() + ";", track_Org);
            }
        }

        public async Task<int> Restrict(SyncActionPayload syncActionPayload)
        {
            bool restrict = false;

            if (syncActionPayload.action == enWorkspaceAction.RESTRICT_TRACK.ToString())
            {
                restrict = true;
            }

            try
            {
                //--- Update elastic index
                _ = Task.Run(() => _elasticLogic.RestraictTracks(syncActionPayload.ids.Select(Guid.Parse).ToArray(), restrict)).ConfigureAwait(false);

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append("update track_org set restricted = @restricted ");
                stringBuilder.AppendFormat(" where id in ({0}) ", string.Join(",", syncActionPayload.ids.Select(s => "'" + s + "'")));

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {                    
                    return await c.ExecuteAsync(stringBuilder.ToString(), new track_org()
                    {
                        restricted = restrict
                    });
                }

            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Restrict Track");
                throw;
            }
        }



        public async Task<int> UpdateOrgData(track_org trackOrg)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update track_org set org_data=CAST(@org_data AS json), change_log=CAST(@change_log AS json),
                last_edited_by=@last_edited_by,date_last_edited=CURRENT_TIMESTAMP
                where original_track_id=@original_track_id and org_id=@org_id;", trackOrg);
            }
        }

        public async Task<int> UpdateChartInfo(TrackChartInfo trackChartInfo, Guid mlTrackId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update track_org set chart_info=CAST(@chart_info AS json),date_last_edited=CURRENT_TIMESTAMP
                where id=@id;", new track_org()
                {
                    chart_info = JsonConvert.SerializeObject(trackChartInfo, new JsonSerializerSettings()),
                    id = mlTrackId
                });
            }
        }

        public async Task<track_org> GetTrackOrgByDhTrackIdAndOrgId(Guid dhTrackId, string orgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<track_org>(@"select * from track_org to2 
                where to2.original_track_id = @original_track_id and org_id = @org_id", new track_org()
                {
                    original_track_id = dhTrackId,
                    org_id = orgId
                });

            }
        }

        public async Task<int> UpdateChangeLog(TrackChangeLog trackChangeLog, Guid? trackOrgId, Guid? dhTrackId, string orgId)
        {
            int result = 0;
            track_org track_Org = null;
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                if (trackOrgId != null)
                {
                    track_Org = await c.QuerySingleOrDefaultAsync<track_org>(@"select * from track_org to2 
                    where to2.id = @id", new track_org()
                    {
                        id = (Guid)trackOrgId
                    });
                }
                else
                {
                    track_Org = await c.QuerySingleOrDefaultAsync<track_org>(@"select * from track_org to2 
                    where to2.original_track_id = @original_track_id and org_id = @org_id;", new track_org()
                    {
                        original_track_id = (Guid)dhTrackId,
                        org_id = orgId
                    });
                }

                if (track_Org != null)
                {
                    List<TrackChangeLog> trackChange = track_Org.change_log == null ? new List<TrackChangeLog>() : JsonConvert.DeserializeObject<List<TrackChangeLog>>(track_Org.change_log);
                    trackChange.Add(trackChangeLog);
                    track_Org.change_log = JsonConvert.SerializeObject(trackChange);

                    result = await c.ExecuteAsync(@"update track_org set change_log=CAST(@change_log AS json),last_edited_by=@last_edited_by,date_last_edited=CURRENT_TIMESTAMP
                        where id=@id;", track_Org);
                }
            }
            return result;
        }

        public async Task<int> GetTrackOrgCountByWsOrgId(Guid wsOrgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(@"select count(1) from track_org to2  
                where to2.org_workspace_id = @org_workspace_id and to2.source_deleted=@source_deleted;", new { org_workspace_id = wsOrgId, source_deleted = false });
            }
        }

        public async Task<track_org> GetById(Guid trackOrgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<track_org>(@"select * from track_org to2 
                where to2.id = @id;", new track_org()
                {
                    id = trackOrgId
                });
            }
        }

        public async Task<int> UpdateChartInfoBulk(List<track_org> trackOrgs)
        {
            int count = 0;
            string sql = "update track_org set chart_info=CAST(@chart_info AS json),date_last_edited=CURRENT_TIMESTAMP where original_track_id=@original_track_id and org_id=@org_id;";
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                foreach (var item in trackOrgs)
                {
                    count += await c.ExecuteAsync(sql, item);
                }
            }
            return count;
        }

        public async Task<int> UpdateChartArtist(bool status, Guid trackId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update track_org set chart_artist=@chart_artist where original_track_id=@original_track_id;", new track_org()
                {
                    chart_artist = status,
                    original_track_id = trackId
                });
            }
        }

        public async Task<int> UpdateTrackContentAlert(bool status, ContentAlert contentAlert, int userid)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"update track_org set content_alert=@content_alert,
                                                content_alerted_date=CURRENT_TIMESTAMP,
                                                content_alerted_user=@content_alerted_user,
                                                alert_type=@alert_type,
                                                alert_note=@alert_note,
                                                last_edited_by=@last_edited_by,
                                                ca_resolved_user=@ca_resolved_user,
                                                ca_resolved_date=@ca_resolved_date
                                                where id=@id;", new track_org()
                    {
                        content_alert = status,
                        id = contentAlert.refId,
                        last_edited_by = userid,
                        alert_type = contentAlert.alertType,
                        alert_note = contentAlert.alertNote,
                        content_alerted_user = userid,
                        ca_resolved_user = null,
                        ca_resolved_date = null
                    });
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "UpdateTrackContentAlert | UserId : {userId}, Object : {@Object}", userid, contentAlert);  
                return 0;
            }            
        }


        public async Task<int> UpdateResolveTrackContentAlert(bool status, ResolveContentAlert contentAlert, int userid)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"update track_org set content_alert=@content_alert,                                                
                                                content_alerted_user=@content_alerted_user,
                                                content_alerted_date=@content_alerted_date,
                                                alert_type=@alert_type,
                                                alert_note=@alert_note,
                                                ca_resolved_user=@ca_resolved_user,
                                                ca_resolved_date=CURRENT_TIMESTAMP,
                                                last_edited_by=@last_edited_by 
                                                where id=@id;", new track_org()
                    {
                        content_alert = status,
                        id = contentAlert.refId,
                        last_edited_by = userid,
                        ca_resolved_user = userid,
                        content_alerted_date = null,
                        content_alerted_user = null,
                        alert_type = null,
                        alert_note = ""
                    });
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "UpdateResolveTrackContentAlert | UserId : {userId}, Object : {@Object}", userid, contentAlert);              
                return 0;
            }            
        }

        public async Task<int> UpdateAlbumContentAlert(bool status, ContentAlert contentAlert, int userid)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"update album_org set content_alert=@content_alert,
                                                content_alerted_date=CURRENT_TIMESTAMP,
                                                content_alerted_user=@content_alerted_user,
                                                alert_type=@alert_type,
                                                alert_note=@alert_note,
                                                last_edited_by=@last_edited_by,
                                                ca_resolved_user=@ca_resolved_user,
                                                ca_resolved_date=@ca_resolved_date
                                                where id=@id;", new album_org()
                    {
                        content_alert = status,
                        id = contentAlert.refId,
                        last_edited_by = userid,
                        content_alerted_user = userid,
                        alert_type = contentAlert.alertType,
                        alert_note = contentAlert.alertNote,
                        ca_resolved_user = null,
                        ca_resolved_date = null
                    });
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "UpdateAlbumContentAlert | UserId : {userId}, Object : {@Object}", userid, contentAlert);
                return 0;
            }            
        }

        public async Task<int> UpdateResolveAlbumContentAlert(bool status, ResolveContentAlert contentAlert, int userid)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"update album_org set content_alert=@content_alert,                                                
                                                content_alerted_user=@content_alerted_user,
                                                content_alerted_date=@content_alerted_date,
                                                alert_type=@alert_type,
                                                alert_note=@alert_note,
                                                ca_resolved_user=@ca_resolved_user,
                                                ca_resolved_date=CURRENT_TIMESTAMP,
                                                last_edited_by=@last_edited_by 
                                                where id=@id;", new album_org()
                    {
                        content_alert = status,
                        id = contentAlert.refId,
                        last_edited_by = userid,
                        ca_resolved_user = userid,
                        content_alerted_date = null,
                        content_alerted_user = null,
                        alert_type = null,
                        alert_note = ""
                    });
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "UpdateResolveAlbumContentAlert | UserId : {userId}, Object : {@Object}", userid, contentAlert);       
                return 0;
            }
            
        }

        public async Task<int> UpdateChartAlbumArtist(bool status, Guid albumId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update album_org set chart_artist=@chart_artist where original_album_id=@original_album_id;", new album_org()
                {
                    chart_artist = status,
                    original_album_id = albumId
                });
            }
        }

        public async Task<int> UpdateTrackOrgByAlbumId(Guid albumId, Guid? trackId)
        {
            track_org trackOrg = new track_org() {
                album_id = albumId
            };

            string sql = @"update track_org set date_last_edited=CURRENT_TIMESTAMP
                where album_id = @album_id ";

            if (trackId != null) {
                trackOrg.id = (Guid)trackId;
                sql += " and id!=@id";
            }

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(sql + ";", trackOrg);
            }
        }

        public async Task<int> GetNewTracksCount(DateTime date, string orgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(@"select count(1) from track_org to2 
where to2.date_created = to2.date_last_edited 
and to2.date_last_edited::date = @date_last_edited
and lower(to2.org_id) = @org_id;", new { date_last_edited = date.Date, org_id = orgId.ToLower() });
            }
        }
    }
}
