using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class LibraryRepository : GenericRepository<library>, ILibraryRepository
    {
        public MLContext _context { get; }
        private readonly ILogger<LibraryRepository> _logger;

        public LibraryRepository(MLContext context, ILogger<LibraryRepository> logger) : base(context)
        {
            _context = context;
            _logger = logger;
        }
        

        public async Task<library_org> SaveLibraryOrg(library_org library_Org)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                string sql = @"INSERT INTO public.library_org(
	org_library_id, library_id, org_id, ml_status, sync_status, restricted, archived, notes, date_created, date_last_edited, created_by, last_edited_by,workspace_id,album_sync_status,last_sync_api_result_id,last_album_sync_api_result_id)
	VALUES (@org_library_id, @library_id, @org_id, @ml_status, @sync_status, @restricted, @archived, @notes, now(), now(), @created_by, @last_edited_by,@workspace_id,@album_sync_status,@last_sync_api_result_id,@last_album_sync_api_result_id) returning *; ";

                return await c.QuerySingleAsync<library_org>(sql, library_Org);
            }
        }

        public void SyncLibraries(int UserId)
        {
            try
            {
                 _context.Database.ExecuteSqlRaw(string.Format("call sp_sync_library('{0}')", UserId));               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncLibraries");
            }
        }

        public async Task<library_org> GetLibraryByOrgId(Guid libraryId, string orgId)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.QuerySingleOrDefaultAsync<library_org>("select * from library_org lo where lo.library_id=@library_id  and lo.org_id=@org_id  limit 1",
                    new library_org() { library_id = libraryId, org_id = orgId });
            }
        }

        public async Task<IEnumerable<library>> GetLibraryListByWorkspaceId(Guid workspaceId)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.QueryAsync<library>("select * from library l where l.workspace_id = @workspace_id and l.archived = false",
                    new library() { workspace_id = workspaceId });
            }
        }

        public async Task<int> UpdateLibraryOrg(library_org library_Org)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("update library_Org set date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by ");

            if (library_Org.ml_status > 0)
                stringBuilder.Append(",ml_status=@ml_status ");

            if (library_Org.sync_status > 0)
                stringBuilder.Append(",sync_status=@sync_status ");

            if (library_Org.album_sync_status > 0)
                stringBuilder.Append(",album_sync_status=@album_sync_status ");

            if (!string.IsNullOrEmpty(library_Org.notes))
                stringBuilder.Append(",notes=@notes ");

            if (library_Org.last_sync_api_result_id !=null)
                stringBuilder.Append(",last_sync_api_result_id=@last_sync_api_result_id ");

            if (library_Org.last_album_sync_api_result_id != null)
                stringBuilder.Append(",last_album_sync_api_result_id=@last_album_sync_api_result_id ");            

            if (library_Org.org_library_id == Guid.Empty)
            {
                stringBuilder.Append(" where library_id=@library_id ");

                if (!string.IsNullOrWhiteSpace(library_Org.org_id))
                    stringBuilder.Append(" and org_id=@org_id ");
            }
            else
            {
                stringBuilder.Append(" where org_library_id=@org_library_id ");
            }           

            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.ExecuteAsync(stringBuilder.ToString(), library_Org);
            }
        }

        public async Task<IEnumerable<library_org>> GetOrgLibraryListByWorkspaceId(Guid workspaceId,string orgId)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.QueryAsync<library_org>(@"select lo.* from library_org lo 
                left join library l2 on lo.library_id = l2.library_id 
                where l2.workspace_id = @workspace_id and org_id=@org_id",
                    new { workspace_id = workspaceId, org_id= orgId });
            }
        }

        public async Task<library> GetById(Guid libraryId)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.QuerySingleOrDefaultAsync<library>("select * from library mmt where library_id=@library_id", new library()
                {
                    library_id = libraryId
                });

            }
        }

        public async Task<IEnumerable<library>> GetNewLiveLibrariesAfterLive()
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.QueryAsync<library>(@"select l.library_id,l.workspace_id from library l 
left join library_org lo on lo.library_id = l.library_id
where l.workspace_id in (
select workspace_id from workspace_org wo
where wo.ml_status = 2
) and lo.org_id is null");
            }
        }

        public async Task<IEnumerable<library>> GetNewAvailableLibrariesAfterLive()
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.QueryAsync<library>(@"select l.library_id,l.workspace_id from library l 
left join library_org lo on lo.library_id = l.library_id
where l.workspace_id in (
select workspace_id from workspace_org wo
where wo.ml_status = 5
) and lo.org_id is null");
            }
        }

        public async Task<IEnumerable<library>> GetNewDistinctWorkspacesAfterLive()
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.QueryAsync<library>(@"select distinct(l.workspace_id)  from library l 
left join library_org lo on lo.library_id = l.library_id
where l.workspace_id in (
select workspace_id from workspace_org wo
where wo.ml_status = 2 or wo.ml_status = 5
) and lo.org_id is null and l.archived = false");
            }
        }

        public async Task<int> UpdateLibraryTrackCount(library library)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                return await c.ExecuteAsync("update \"library\" set ml_track_count=@ml_track_count where library_id =@library_id;", library);
            }
        }
    }
}
