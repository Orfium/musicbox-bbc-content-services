using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Nest;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class LogElasticTrackChangeRepository : GenericRepository<elastic_track_change>, ILogElasticTrackChangeRepository
    {
        private readonly ILogger<LogElasticTrackChangeRepository> _logger;
        private readonly IOptions<AppSettings> _appSettings;

        public LogElasticTrackChangeRepository(MLContext context, ILogger<LogElasticTrackChangeRepository> logger,
            IOptions<AppSettings> appSettings) : base(context)
        {
            Context = context;
            _logger = logger;
            _appSettings = appSettings;
        }

        public MLContext Context { get; }

        public async Task<IEnumerable<LogElasticTrackChange>> Search(int pageSize, Guid orgWorkspaceId, int retries)
        {            
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryAsync<LogElasticTrackChange>(@"select etc.id,etc.document_id,etc.track_org_data,mmt.dh_version_id,mmt.metadata,etc.date_created,w.workspace_name,l.library_name,
mmt.external_identifiers,etc.deleted,mmt.source_ref,mmt.ext_sys_ref,COALESCE(ow.ws_type, ""left""('External'::text, 10)::character varying) AS ws_type,
etc.archived,mmt.edit_track_metadata,mmt.edit_album_metadata,mmt.pre_release,etc.org_id,etc.restricted,mma.metadata as album_metadata,mma.album_id
from log.elastic_track_change etc
left join public.ml_master_track mmt on etc.original_track_id = mmt.track_id 
left join public.workspace w on mmt.workspace_id = w.workspace_id 
left join org_workspace ow ON w.workspace_id = ow.dh_ws_id
left join public.library l  on mmt.library_id = l.library_id 
left join ml_master_album mma on mmt.album_id = mma.album_id 
where etc.org_workspace_id = @orgWorkspaceId
order by etc.id
LIMIT @pageSize",
                     new { pageSize, orgWorkspaceId });

                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "LogElasticTrackChange Search | Retry attempt: {Retry} | Module: {Module}", retries, "Track Sync");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    await Search( pageSize, orgWorkspaceId, --retries);
                }
                _logger.LogError(ex, "LogElasticTrackChange Search | Module: {Module}", "Track Sync");
            }
            return null;
        }

        public async Task<IEnumerable<LogElasticAlbumChange>> SearchElasticAlbumChange(int pageSize, Guid orgWorkspaceId, int retries)
        {           
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryAsync<LogElasticAlbumChange>(@"select eac.document_id,mma.album_id,mma.metadata,eac.album_org_data,mma.workspace_id,mma.library_id,w.workspace_name,
 l.library_name,l.notes as library_notes,eac.restricted,eac.archived,mma.archived as deleted,
 COALESCE(ow.ws_type, ""left""('External'::text, 10)::character varying) AS ws_type
from log.elastic_album_change eac 
left join public.ml_master_album mma on eac.original_album_id = mma.album_id 
left join public.workspace w on mma.workspace_id = w.workspace_id
left join org_workspace ow ON w.workspace_id = ow.dh_ws_id
left join public.library l  on mma.library_id = l.library_id 
where eac.org_workspace_id = @orgworkspaceid
LIMIT @pagesize",
                     new { pageSize, orgWorkspaceId });
                    
                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "SearchElasticAlbumChange Search | Retry attempt: {Retry} | Module: {Module}", retries, "Track Sync");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    await Search( pageSize, orgWorkspaceId, --retries);
                }
                _logger.LogError(ex, "SearchElasticAlbumChange Search | Module: {Module}", "Track Sync");
            }
            return null;            
        }

        public async Task<int> BulkDelete(Guid orgWorkspaceId,long lastIndexId)
        {
            try
            {               
                using (var c = new NpgsqlConnection(Context.Database.GetDbConnection().ConnectionString))
                {                   
                    var result = await c.ExecuteAsync("delete from log.elastic_track_change where id <= @id and org_workspace_id=@org_workspace_id",
                        new elastic_track_change() { id= lastIndexId ,org_workspace_id= orgWorkspaceId });

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return 0;
        }

        public async Task<int> BulkDeleteAlbums(List<MLAlbumDocument> logElasticAlbumChanges)
        {
            try
            {
                string sql = string.Empty;
                string joined = string.Join(",", logElasticAlbumChanges.Select(x => x.id));
                var toDelete = string.Join(", ", joined.Split(",").Select(x => $"'{x}'"));
                using (var c = new NpgsqlConnection(Context.Database.GetDbConnection().ConnectionString))
                {
                    sql = string.Format(@"delete from log.elastic_album_change where document_id in ({0})", toDelete);
                    var result = await c.ExecuteAsync(sql);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return 0;
        }
        
        public async Task LogErrors(List<log_track_index_error> log_Track_Index_Errors)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    foreach (var item in log_Track_Index_Errors)
                    {
                        await c.ExecuteAsync(@"INSERT INTO log.log_track_index_error(doc_id, error, reson)VALUES(@doc_id, @error, @reson);", item);
                    }                   
                }
            }
            catch (Exception)
            {
                throw;
            }            
        }
    }
}
