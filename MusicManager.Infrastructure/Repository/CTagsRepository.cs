using Dapper;
using Microsoft.EntityFrameworkCore;
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
    public class CTagsRepository : GenericRepository<c_tag>, ICTagsRepository
    {
        public MLContext _context { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public CTagsRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
        }
        public void AddMemberLabelData(member_label member_label)
        {
            _context.member_label.Add(member_label);
            _context.SaveChanges();
        }

        public void AddPriorApprovalWork(prior_approval_work prior_approval_work)
        {
            _context.prior_approval_work.Add(prior_approval_work);
            _context.SaveChanges();
        }

        public async Task<int> ChangeStatus(int id, int status)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync("update c_tag set status=@status where id=@id;", new c_tag { id = id, status = status });
            }
        }

        public async Task<IEnumerable<c_tag>> GetAllActiveCtags()
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryAsync<c_tag>("select * from c_tag where status=1");
                }
            }
            catch (Exception ex)
            {

                throw;
            }
           
        }

        public async Task<c_tag> SaveCtag(c_tag c_tag)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    string q = (@"INSERT INTO public.c_tag(id,created_by, date_created, description, name, type, colour,indicator,display_indicator, date_last_edited, last_edited_by,status,group_id)
                                    VALUES(@id,@created_by, @date_created, @description, @name, @type, @colour,@indicator,@display_indicator,@date_last_edited,@last_edited_by,@status,@group_id) returning *"
                                  );
                    var result = await c.QuerySingleAsync<c_tag>(q, c_tag);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }


        public async Task<int> SavePrsSearchTime(log_prs_search_time log_prs_search_time)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    string q = (@"INSERT INTO log.log_prs_search_time(time, search_type, search_query, track_id,date_created )
                                    VALUES(@time, @search_type, @search_query, @track_id, @date_created)"
                                  );
                    return await c.ExecuteAsync(q, log_prs_search_time);
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public async Task<int> UpdateCtag(c_tag c_tag)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync(@"update c_tag set description=@description, name=@name, type=@type, colour=@colour,date_last_edited=CURRENT_TIMESTAMP, last_edited_by=@last_edited_by,display_indicator=@display_indicator,
                        indicator=@indicator,group_id=@group_id where id=@id;", c_tag);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<c_tag> GetCtagById(int id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<c_tag>("select * from c_tag ct where ct.id = @id", new c_tag() { id = id });
            }
        }

        public async Task<c_tag> GetCtagByRuleId(int id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<c_tag>(@"select ct.* from c_tag ct
                left join c_tag_extended cte on ct.id = cte.c_tag_id
                where cte.id = @id; ", new c_tag_extended() { id = id });
            }
        }

        public async Task<IEnumerable<c_tag>> GetDynamicDisplayCtags()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<c_tag>("select * from c_tag ct where ct.display_indicator = true and ct.status = 1");
            }
        }

        public async Task<string> GetTunecodeByISRC(string isrc)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<string>(@"select tunecode from staging.isrc_tunecode
                where isrc = @isrc",new { isrc = isrc });
            }
        }

        public async Task<int> UpdateCtagIndexStatus(c_tag_index_status cTagIndexStatus)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
               return await c.ExecuteAsync(@"INSERT INTO public.c_tag_index_status(type, updated_on, updated_by, update_idetifier)
                VALUES(@type, CURRENT_TIMESTAMP, @updated_by, @update_idetifier)
                on conflict(type) do update set updated_on = CURRENT_TIMESTAMP,
	            updated_by = @updated_by,update_idetifier = @update_idetifier;", cTagIndexStatus);
            }            
        }

        public async Task<IEnumerable<c_tag>> GetIndexedCtags()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<c_tag>("select * from c_tag ct where ct.type = 'indexed'");
            }
        }

        public async Task<c_tag> GetArchiveIndexedCtag()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefault(@"select * from c_tag ct where ct.type = 'indexed'
                and lower(ct.name) like '%archive%'
                limit 1 ");
            }
        }

        public async Task<c_tag_index_status> GetCtagIndexStatusByType(string type)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<c_tag_index_status>(@"select * from public.c_tag_index_status where type=@type;", new c_tag_index_status() { type = type });
            }
        }

        public async Task<bool> CheckPPLLabelContains(string label)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("select 1 from ppl_label_search pls where ");
                stringBuilder.AppendFormat(" pls.label ~* '( |^){0}([^A-z]|$)' or pls.member ~* '( |^){0}([^A-z]|$)' ", label.Replace("'", "''").Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)"));
                stringBuilder.AppendFormat(" limit 1;");
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    if (await c.ExecuteScalarAsync<int>(stringBuilder.ToString()) > 0) {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
            return false;
        }

        public async Task<bool> CheckPPLLabelExact(string label)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("select 1 from ppl_label_search pls where ");
                stringBuilder.AppendFormat(" lower(pls.label)='{0}' or lower(pls.member)='{0}'", label.Replace("'", "''").Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)"));
                stringBuilder.AppendFormat(" limit 1;");
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    if (await c.ExecuteScalarAsync<int>(stringBuilder.ToString()) > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
            return false;
        }
    }
}
