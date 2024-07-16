using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class CTagsExtendedRepository : GenericRepository<c_tag_extended>, ICTagsExtendedRepository
    {
        public MLContext _context { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public CTagsExtendedRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
        }

        public IEnumerable<c_tag_extended> GetCtagExtendedByCTagId(int cTagId)
        {
            throw new NotImplementedException();
        }

        public async Task<int> UpdateCtagExtended(c_tag_extended ctag_extended)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(string.Format("update c_tag_extended set condition=CAST(@condition AS json),name=@name,color=@color,date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by,status=@status,notes=@notes,c_tag_id=@c_tag_id where id=@id;"), ctag_extended);
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<List<c_tag_extended>> GetActiveRules(int id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                var list = await c.QueryAsync<c_tag_extended>(@"select * from c_tag_extended where status=1 and c_tag_id=@c_tag_id", 
                    new c_tag_extended { c_tag_id = id });
                return list?.ToList();
            }
        }

        public async Task<c_tag_extended> GetRuleByTrackId(string trackId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryFirstOrDefaultAsync<c_tag_extended>("select * from c_tag_extended where track_id=@track_id", new c_tag_extended { track_id = trackId });
            }
        }

        public async Task<int> ChangeRuleStatus(int id, int status)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync("update c_tag_extended set status=@status where id=@id;", new c_tag { id = id, status = status });
            }
        }

        public async Task<int> DeleteRule(int id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync("delete from c_tag_extended where id=@id;", new c_tag_extended { id = id });
            }
        }

        public async Task<c_tag_extended> SaveCtagExtended(c_tag_extended c_tag)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    c_tag_extended result = null;

                    long count = await c.ExecuteScalarAsync<long>(@"select count(*) from public.c_tag_extended c
                    where c.c_tag_id = @c_tag_id and c.track_id = @track_id;", c_tag);

                    if (count <= 0) {
                        string q = (@"INSERT INTO public.c_tag_extended(date_created,condition,created_by, c_tag_id, name,  color, status, date_last_edited, last_edited_by,track_id, notes)
                                    VALUES(@date_created,CAST(@condition AS json),@created_by, @c_tag_id, @name,  @color,@status, @date_last_edited,@last_edited_by,@track_id, @notes) returning *"
                                      );
                        result = await c.QuerySingleAsync<c_tag_extended>(q, c_tag);
                    }
                    
                    return result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
