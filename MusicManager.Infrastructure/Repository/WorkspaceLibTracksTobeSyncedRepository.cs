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
    public class WorkspaceLibTracksTobeSyncedRepository : GenericRepository<ws_lib_tracks_to_be_synced>, IWorkspaceLibTracksTobeSyncedRepository
    {
        public MLContext _context { get; }
        public WorkspaceLibTracksTobeSyncedRepository(MLContext context) : base(context)
        {
            _context = context;
        }

        public MLContext Context { get; }

        public async Task<string> GetMlStatusByType(Guid refId, string type)
        {
            StringBuilder _SB = new StringBuilder();

            if (type == "w")
            {
                _SB.AppendFormat(@"select w.ml_status from workspace w 
                where w.workspace_id = '{0}'", refId);
            }
            else if (type == "l")
            {
                _SB.AppendFormat(@"select l.ml_status from library l 
                where l.library_id = '{0}'", refId);
            }

            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                var result = await c.ExecuteScalarAsync(_SB.ToString());
                return result.ToString();
            }
           
        }

        public async Task<List<ws_lib_tracks_to_be_synced>> GetToBeSyncedList()
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {

                StringBuilder _SB = new StringBuilder();
                _SB.Append(@"select * from ws_lib_tracks_to_be_synced
                           where status = 'created' or status = 'synced' or status = 'syncing' or status = 'reindex' or status = 'reindxng'
                           order by type");
                var result = await c.QueryAsync<ws_lib_tracks_to_be_synced>(_SB.ToString());
                return  result.ToList();
            }
        }

        public async Task<int> UpdateStatus(string status, Guid ref_id)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {

                StringBuilder _SB = new StringBuilder();
                _SB.Append("update public.ws_lib_tracks_to_be_synced ");
                _SB.AppendFormat("set status = '{0}' ", status);
                _SB.AppendFormat("where ref_id='{0}'", ref_id);

                var result = await c.ExecuteAsync(_SB.ToString());
                return result;
            }
        }

        public async Task<ws_lib_tracks_to_be_synced> GetByRefId(Guid refId, string type)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                string sql = string.Format(@"select * from ws_lib_tracks_to_be_synced where ref_id='{0}' and type='{1}' limit 1", refId, type);
                return await c.QuerySingleOrDefaultAsync<ws_lib_tracks_to_be_synced>(sql);               
            }
        }

        public async Task RemoveUnwantedEntries(Guid refId, string type, enMLStatus enMLStatus)
        {
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                switch (enMLStatus)
                {
                    case enMLStatus.Archive:
                        if (type == "w")
                        {
                            await c.ExecuteAsync(string.Format(@"delete from ws_lib_tracks_to_be_synced where ref_id = '{0}'
                            or ref_id in (
                                select l.library_id from public.library l where l.workspace_id = '{0}'
                            )", refId));
                        }
                        else {
                            await c.ExecuteAsync(string.Format(@"delete from ws_lib_tracks_to_be_synced where ref_id = '{0}'", refId));
                        }
                        break;                 
                    default:
                        break;
                }               
            }

            
        }
    }
}
