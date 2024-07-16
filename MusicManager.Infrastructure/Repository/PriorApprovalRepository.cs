using Dapper;
using Microsoft.EntityFrameworkCore;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class PriorApprovalRepository : GenericRepository<prior_approval_work>, IPriorApprovalRepository
    {
        public MLContext _context { get; }

        public PriorApprovalRepository(MLContext context) : base(context)
        {
            _context = context;
        }

        public async Task<prior_approval_work> SavePriorApproval(prior_approval_work prior_Approval)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                string sql = @"INSERT INTO public.prior_approval_work(
                             date_created, created_by, date_last_edited, last_edited_by, ice_mapping_code, local_work_id, tunecode, 
                            iswc, work_title, composers, publisher, matched_isrc, matched_dh_ids, broadcaster,artist,writers)
	                        VALUES(now(), @created_by, now(), @last_edited_by, @ice_mapping_code, @local_work_id, @tunecode,
                                     @iswc, @work_title, @composers, @publisher, @matched_isrc, @matched_dh_ids ,@broadcaster, @artist, @writers) returning *; ";

                return await c.QuerySingleAsync<prior_approval_work>(sql, prior_Approval);
            }
        }

        public async Task<int> UpdatePriorApproval(prior_approval_work prior_Approval)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                string sql = @"Update public.prior_approval_work set date_last_edited=now(),
                              last_edited_by=@last_edited_by, ice_mapping_code=@ice_mapping_code, local_work_id=@local_work_id, tunecode=@tunecode, 
                            iswc=@iswc, work_title=@work_title, composers=@composers, publisher=@publisher, matched_isrc=@matched_isrc, matched_dh_ids=@matched_dh_ids, broadcaster=@broadcaster,
                            artist=@artist, writers=@writers where id=@id";


                return await c.ExecuteAsync(sql, prior_Approval);
            }
        }
    }
}
