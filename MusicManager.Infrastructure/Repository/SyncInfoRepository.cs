using Dapper;
using Microsoft.EntityFrameworkCore;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class SyncInfoRepository : GenericRepository<sync_info>, ISyncInfoRepository
    {
        public SyncInfoRepository(MLContext context) : base(context)
        {
            _context = context;
        }

        public MLContext _context { get; }

        public async Task<sync_info> GetLastSyncRecord(string type, Guid workspaceId)
        {
            try
            {
                string _sql = string.Format(@"select * from sync_info si 
                where si.workspace_id = '{0}' and si.type ='{1}'
                order by si.date_created desc
                limit 1", workspaceId, type);

                using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    return await c.QueryFirstOrDefaultAsync<sync_info>(_sql);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
           
        }
    }
}
