using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using Npgsql;
using Snickler.EFCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class WorkspaceRepository : GenericRepository<workspace>, IWorkspaceService
    {
        private readonly ILogger<WorkspaceRepository> _logger;
        private readonly IOptions<AppSettings> _appSettings;
        public MLContext _context { get; }     


        public WorkspaceRepository(MLContext context, ILogger<WorkspaceRepository> logger,
            IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _logger = logger;
            _appSettings = appSettings;            
        }

        public async Task<IEnumerable<workspace>> GetWorkspacesForSyncAsync(int retries = 2)
        {
            string _sql = string.Format(@"select * from workspace w 
where w.priority_sync=0 and w.workspace_id in (
select distinct(wo.workspace_id) from workspace_org wo 
where wo.ml_status in (2,3,4)) or w.workspace_id in (
select distinct(lo.workspace_id) from library_org lo 
where lo.ml_status in (2,3,4))");

            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryAsync<workspace>(_sql);
                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "GetWorkspacesForSyncAsync | Retry attempt: {Retry} | Module: {Module}", retries, "Track Sync");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    await GetWorkspacesForSyncAsync(retries - 1);
                }
                _logger.LogError(ex, "GetWorkspacesForSyncAsync | SQL: {Sql} | Module: {Module}", _sql, "Track Sync");
            }
            return null;
        }

        public async Task<IEnumerable<workspace>> GetMasterWorkspaceForSyncAsync(Guid masterWorkspaceId, int retries = 2)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryAsync<workspace>(@"select * from workspace w 
                left join workspace_org wo on w.workspace_id = wo.workspace_id 
                where w.priority_sync > 0 or (wo.ml_status >= 2 and w.workspace_id = @workspace_id);", new { workspace_id = masterWorkspaceId });
                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "GetMasterWorkspaceForSyncAsync | Retry attempt: {Retry} | Module: {Module}", retries, "Master - Track Sync");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    await GetMasterWorkspaceForSyncAsync(masterWorkspaceId, retries - 1);
                }
                _logger.LogError(ex, "GetMasterWorkspaceForSyncAsync | Module: {Module}", "Master - Track Sync");
            }
            return null;
        }

        public void SyncWorkspaces(int userId)
        {
            try
            {                
                _context.Database.ExecuteSqlRaw(string.Format("call sp_sync_workspace('{0}')", userId));               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncWorkspaces");
            }

        }

        public async Task<int> AddOrgWorkspaceStatus(SyncActionPayload syncActionPayload, string type)
        {
            foreach (var item in syncActionPayload.ids)
            {
                int count = await _context.org_workspace.CountAsync(a => a.dh_ws_id == new Guid(item));
                if (count == 0)
                {
                    _context.org_workspace.Add(new org_workspace()
                    {
                        created_by = int.Parse(syncActionPayload.userId),
                        date_created = DateTime.Now,
                        dh_ws_id = new Guid(item),
                        organization = syncActionPayload.orgid,
                        ws_type = type
                    });
                }
            }           
            return await _context.SaveChangesAsync();
        }



        public async Task<int> UpdateWorkspace(workspace workspace)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("update workspace set date_last_edited=now() ");

                if (workspace.download_status > 0)
                    stringBuilder.Append(", download_status=@download_status ");

                if (!string.IsNullOrEmpty(workspace.next_page_token))
                    stringBuilder.Append(", next_page_token=@next_page_token ");

                if (!string.IsNullOrEmpty(workspace.album_next_page_token))
                    stringBuilder.Append(", album_next_page_token=@album_next_page_token ");

                if (workspace.dh_status > 0)
                    stringBuilder.Append(", dh_status=@dh_status ");

                if (workspace.ml_track_count != null)
                    stringBuilder.Append(", ml_track_count=@ml_track_count ");

                stringBuilder.Append(" where workspace_id = @workspace_id");

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(stringBuilder.ToString(), workspace);
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public async Task<enWorkspaceType> GetWorkspaceType(string wsId, string orgId)
        {
            org_workspace org_Workspace = null;
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                org_Workspace = await c.QuerySingleOrDefaultAsync<org_workspace>("select * from org_workspace where dh_ws_id=@dh_ws_id and organization=@organization",
                    new org_workspace() { dh_ws_id = Guid.Parse(wsId), organization = orgId });
            }

            if (org_Workspace == null)
            {
                return enWorkspaceType.External;
            }
            else
            {
                return (enWorkspaceType)Enum.Parse(typeof(enWorkspaceType), org_Workspace.ws_type);
            }
        }

        public async Task<workspace_org> SaveWorkspaceOrg(workspace_org workspace_Org)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                string sql = @"INSERT INTO public.workspace_org(
                            org_workspace_id, workspace_id, org_id, ml_status, sync_status, restricted, archived, notes, date_created, date_last_edited, created_by, last_edited_by,index_status,album_sync_status,last_sync_api_result_id,last_album_sync_api_result_id)
	                        VALUES(@org_workspace_id, @workspace_id, @org_id, @ml_status, @sync_status, @restricted, @archived, @notes, now(), now(), @created_by, @last_edited_by,@index_status,@album_sync_status,@last_sync_api_result_id,@last_album_sync_api_result_id) returning *; ";

                return await c.QuerySingleAsync<workspace_org>(sql, workspace_Org);
            }
        }

        public async Task<workspace_org> GetWorkspaceOrgByOrgId(Guid workspaceId, string orgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<workspace_org>("select * from workspace_org wo where wo.workspace_id=@workspace_id and wo.org_id=@org_id limit 1",
                    new workspace_org() { workspace_id = workspaceId, org_id = orgId });
            }
        }

        public async Task<int> WorkspacePause(SyncActionPayload pauseActionPayload)
        {
            string sql = string.Empty;
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                sql = @"INSERT INTO public.workspace_pause(
                            id, workspace_id, date_created, created_by, last_download_status)
	                        VALUES(@id, @workspace_id, now(), @created_by, @last_download_status); ";

                var res = await c.ExecuteAsync(sql,
                    new workspace_pause { id = Guid.NewGuid(), workspace_id = Guid.Parse(pauseActionPayload.ids[0]), created_by = Convert.ToInt32(pauseActionPayload.userId), last_download_status = Convert.ToInt32(pauseActionPayload.type) });
                return res;
            }
        }

        public async Task<int> WorkspaceContinue(SyncActionPayload pauseActionPayload)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"DELETE FROM public.workspace_pause where workspace_id=@workspace_id; ", new workspace_pause { id = Guid.NewGuid(), workspace_id = Guid.Parse(pauseActionPayload.ids[0]), created_by = Convert.ToInt32(pauseActionPayload.userId) });
            }
        }
        public async Task<int> UpdateDownloadStatus(SyncActionPayload pauseActionPayload, enLibWSDownloadStatus downloadStatus)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update workspace set download_status=@download_status where workspace_id=@workspace_id", new workspace { workspace_id = Guid.Parse(pauseActionPayload.ids[0]), download_status = (int)downloadStatus });
            }
        }

        public async Task<int> GetPreviosStatusFromPause(Guid wsId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                var res = await c.QueryFirstAsync<workspace_pause>(@"select * from workspace_pause where workspace_id=@workspace_id", new workspace_pause { workspace_id = wsId });
                return res.last_download_status;
            }
        }

        public async Task<int> GetLiveWorkspaceCount()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                var res = await c.ExecuteAsync(@"select count(*) from workspace_org wo  where ml_status= 2");
                return res;
            }
        }

        public async Task<int> GetAvlWorkspaceCount()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                var res = await c.ExecuteAsync(@"select count(*) from workspace_org wo  where ml_status= 3");
                return res;
            }
        }

        public async Task<bool> CheckPause(Guid wsId)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteScalarAsync<bool>("select 1 from workspace_pause wo where wo.workspace_id=@workspace_id",
                        new workspace_pause() { workspace_id = wsId, });
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<int> UpdateWorkspaceOrg(workspace_org workspace_Org)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("update workspace_org set date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by ");

            if (workspace_Org.ml_status != null && workspace_Org.ml_status > 0)
                stringBuilder.Append(",ml_status=@ml_status ");

            if (!string.IsNullOrEmpty(workspace_Org.notes))
                stringBuilder.Append(",notes=@notes ");

            if (workspace_Org.sync_status != null && workspace_Org.sync_status > 0)
                stringBuilder.Append(",sync_status=@sync_status ");

            if (workspace_Org.index_status != null && workspace_Org.index_status > 0)
                stringBuilder.Append(",index_status=@index_status ");


            if (workspace_Org.album_sync_status != null && workspace_Org.album_sync_status > 0)
                stringBuilder.Append(",album_sync_status=@album_sync_status ");

            if (workspace_Org.last_sync_api_result_id != null)
                stringBuilder.Append(",last_sync_api_result_id=@last_sync_api_result_id ");

            if (workspace_Org.last_album_sync_api_result_id != null)
                stringBuilder.Append(",last_album_sync_api_result_id=@last_album_sync_api_result_id ");

            if (workspace_Org.org_workspace_id == Guid.Empty)
            {
                stringBuilder.Append(" where workspace_id=@workspace_id ");

                if (!string.IsNullOrWhiteSpace(workspace_Org.org_id))
                    stringBuilder.Append(" and org_id=@org_id ");
            }
            else
            {
                stringBuilder.Append(" where org_workspace_id=@org_workspace_id ");
            }

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(stringBuilder.ToString(), workspace_Org);
            }
        }

        public async Task<int> UpdateMusicOrigin(workspace_org workspace_Org)
        {
            string q = "update workspace_org set music_origin=@music_origin where org_workspace_id=@org_workspace_id";
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(q, workspace_Org);
            }
        }

        public async Task<IEnumerable<workspace_org>> GetWorkspaceOrgs(string orgId, int mlStatusId)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select * from workspace_org wo where 1=1");

            if (!string.IsNullOrWhiteSpace(orgId))
                stringBuilder.Append(" and wo.org_id=@org_id ");

            if (mlStatusId > 0)
                stringBuilder.Append(" and wo.ml_status=@ml_status ");

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<workspace_org>(stringBuilder.ToString(),
                    new workspace_org() { org_id = orgId, ml_status = mlStatusId });
            }
        }

        public async Task<workspace_org> GetWorkspaceOrgById(Guid workspaceOrgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<workspace_org>("select * from workspace_org wo where wo.org_workspace_id=@org_workspace_id",
                    new workspace_org() { org_workspace_id = workspaceOrgId });
            }
        }

        public async Task<workspace> GetWorkspaceById(Guid workspaceId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<workspace>("select * from workspace where workspace_id=@workspace_id",
                    new workspace() { workspace_id = workspaceId });
            }
        }

        public async Task<IEnumerable<workspace_org>> GetWorkspaceOrgsByWorkspaceId(Guid workspaceId)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryAsync<workspace_org>("select * from workspace_org wo where wo.workspace_id=@workspace_id",
                        new workspace_org() { workspace_id = workspaceId });
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<org_workspace> GetOrgWorkspaceByOrgId(string orgId, string type)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<org_workspace>("select * from org_workspace ow where ow.ws_type=@ws_type and ow.organization=@organization",
                    new org_workspace() { ws_type = type, organization = orgId });
            }
        }

        public async Task<int> SetRestricted(SyncActionPayload syncActionPayload)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();

                if (syncActionPayload.type == "track")
                {
                    stringBuilder.Append("update track_org set restricted = true ");
                    stringBuilder.AppendFormat(" where id in ({0}) ", string.Join(",", syncActionPayload.ids.Select(s => "'" + s + "'")));
                }

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    int cd = await c.ExecuteAsync(stringBuilder.ToString());
                    return cd;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> SetRedownloadWorkspace(SyncActionPayload syncActionPayload)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    string nextPageToken = null;

                    if (!string.IsNullOrWhiteSpace(syncActionPayload.type))
                    {
                        object npt = await c.ExecuteScalarAsync(string.Format(@"select s.page_token from log.log_track_sync_session s
                        where s.workspace_id = '{0}'
                        and(s.page_token->> 'dateModified')::date >= '{1}'
                        order by s.session_id
                        limit 1;", syncActionPayload.ids[0], syncActionPayload.type));

                        if(npt!=null)
                            nextPageToken = npt.ToString();
                    }

                    int result = await c.ExecuteAsync("update workspace set next_page_token = @next_page_token, date_last_edited=CURRENT_TIMESTAMP,download_status=@download_status where workspace_id = @workspace_id;",
                        new workspace() { download_status=1, next_page_token = nextPageToken, workspace_id= Guid.Parse(syncActionPayload.ids[0])});

                    return result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> AddOrgWorkspace(SyncActionPayload syncActionPayload, string type)
        {
            try
            {
                foreach (var item in syncActionPayload.ids)
                {
                    org_workspace org_Workspace = new org_workspace()
                    {
                        created_by = int.Parse(syncActionPayload.userId),
                        date_created = DateTime.Now,
                        dh_ws_id = new Guid(item),
                        organization = syncActionPayload.orgid,
                        ws_type = type
                    };

                    using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                    {
                        int count = await c.ExecuteScalarAsync<int>(@"select count(1) from org_workspace ow 
                    where ow.organization = @organization and ow.dh_ws_id = @dh_ws_id;", org_Workspace);

                        if (count == 0)
                        {
                            await c.ExecuteAsync(@"insert into org_workspace (created_by,date_created,dh_ws_id,organization,ws_type) 
                        values(@created_by,@date_created,@dh_ws_id,@organization,@ws_type)", org_Workspace);
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RemoveOrgWorkspace(SyncActionPayload syncActionPayload, string type)
        {
            try
            {
                foreach (var item in syncActionPayload.ids)
                {
                    using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                    {
                        await c.ExecuteAsync(@"delete from org_workspace where organization=@organization and dh_ws_id=@dh_ws_id;", new org_workspace()
                        {
                            dh_ws_id = new Guid(item),
                            organization = syncActionPayload.orgid
                        });
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task<IEnumerable<workspace>> GetPriorityWorkspacesForSyncAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<int> UpdateLastSyncTime(workspace workspace)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync("update workspace set last_sync_date=CURRENT_TIMESTAMP where workspace_id=@workspace_id;", workspace);
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public async Task<int> GetWorkspaceActiveTrackCount(Guid workspaceId)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteScalarAsync<int>(@"select count(1) from ml_master_track mmt  
                where mmt.workspace_id = @workspace_id and mmt.deleted = @deleted", new { workspace_id = workspaceId, deleted = false });
                }
            }
            catch (Exception)
            {
                return 0;
            }            
        }
    }
}
