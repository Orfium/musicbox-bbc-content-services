using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class AlbumRepository : GenericRepository<album>, IAlbumRepository
    {
        public MLContext _context { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public AlbumRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
        }

        public async Task CheckAndArchiveAlbums()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                IEnumerable<Guid> albums = await c.QueryAsync<Guid>(@"select distinct(ao.original_album_id) 
                FROM (select original_album_id from album_org where archive <> true) ao 
                left join track_org to2 ON ao.original_album_id = to2.album_id 
                where to2.id is null");

                foreach (var item in albums)
                {
                    await c.ExecuteAsync(@"update album_org set date_last_edited=CURRENT_TIMESTAMP,archive=true  
                    where original_album_id = @original_album_id;", new album_org() { original_album_id = item });
                }
            }
        }


        public async Task<List<Album>> GetAlbumsForES(Album album)
        {
            try
            {
                SearchData<List<ws_and_lib>> obj = new SearchData<List<ws_and_lib>>();
                obj.Data = new List<ws_and_lib>();

                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append(@"select a.*,w.workspace_name,l.library_name,l.notes as library_notes from album a 
            left join public.workspace w on(a.value ->> 'workspaceId')::uuid = w.workspace_id
            left join public.library l  on(a.value -> 'trackData' ->> 'libraryId')::uuid = l.library_id ");
                stringBuilder.Append("where 1=1 ");

                if (album.workspace_id != null)
                    stringBuilder.Append("and a.workspace_id=@workspace_id ");

                if (album.library_id != null)
                    stringBuilder.Append("and a.library_id=@library_id ");

                if (album.date_last_edited != null)
                    stringBuilder.Append("and a.date_last_edited>@date_last_edited ");

                stringBuilder.Append(" order by a.date_last_edited");

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    var date = await c.QueryAsync<Album>(stringBuilder.ToString(), album);
                    return date.ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> UpdateOrgData(album_org albumOrg)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(@"update album_org set org_data=CAST(@org_data AS json),change_log=CAST(@change_log AS json),
                date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by 
                where original_album_id=@original_album_id and org_id=@org_id",   albumOrg);
            }
        }

        public async Task<album_org> GetAlbumOrgByDhAlbumIdAndOrgId(Guid dhAlbumId, string orgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<album_org>(@"select * from album_org to2 
                where to2.original_album_id = @original_album_id and to2.org_id = @org_id", new album_org()
                {
                    original_album_id = dhAlbumId,
                    org_id = orgId
                });

            }
        }

        public async Task<album_org> GetAlbumOrgById(Guid id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<album_org>(@"select * from album_org to2 
                where to2.id = @id", new album_org()
                {
                    id = id
                });

            }
        }



        public async Task<ml_master_album> GetMlMasterAlbumById(Guid id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<ml_master_album>(@"select * from ml_master_album mma 
                where mma.album_id = @album_id", new ml_master_album()
                {
                    album_id = id
                });

            }
        }

        public async Task<int> GetOrgAlbumCountByWsOrgId(Guid wsOrgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(@"select count(1) from album_org ao   
                where ao.org_workspace_id = @org_workspace_id and ao.source_deleted=@source_deleted;", new { org_workspace_id = wsOrgId, source_deleted=false });
            }
        }

        public async Task<int> GetMasterAlbumCountByWsId(Guid workspaceId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(@"select count(1) from ml_master_album mma    
                where mma.workspace_id = @workspace_id and mma.archived=@archived;", new { workspace_id = workspaceId, archived = false });
            }
        }

        public async Task<IEnumerable<Guid>> GetDistinctAlbumIdFromTrackIds(List<Guid> trackIds)
        {

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<Guid>(string.Format(@"select distinct(original_album_id) from album_org ao 
                left join track_org to2 on ao.original_album_id = to2.album_id 
                where to2.id in ({0}) or to2.original_track_id in ({0})", string.Join(",", trackIds.Select(s => "'" + s.ToString() + "'"))));
            }
        }

        public async Task CheckAndUpdatePreviousAlbumOrgs(List<Guid> albumIds)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    foreach (var item in albumIds)
                    {
                        int count = await c.ExecuteScalarAsync<int>(@"select count(1) from upload_track ut 
                        where ut.dh_album_id = @dh_album_id", new { dh_album_id = item });

                        if (count == 0)
                        {
                            await c.ExecuteScalarAsync<int>(@"delete from upload_album 
                             where dh_album_id = @dh_album_id", new { dh_album_id = item });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<int> UpdateChartInfoById(List<album_org> albumOrgs)
        {
            int count = 0;

            string sql = "update album_org set chart_info=CAST(@chart_info AS json),date_last_edited=CURRENT_TIMESTAMP where original_album_id=@original_album_id and org_id=@org_id;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                foreach (var item in albumOrgs)
                {
                    count += await c.ExecuteAsync(sql, item);
                }
            }
            return count;
        }
    }
}
