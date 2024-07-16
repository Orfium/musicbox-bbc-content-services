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
    public class UploadSessionRepository : GenericRepository<upload_session>, IUploadSessionRepository
    {
        public UploadSessionRepository(MLContext context) : base(context)
        {
            _context = context;
        }
        public MLContext _context { get; }

        public async Task<upload_session> CreateSession(string userId, string orgId)
        {
            upload_session upload_Session = new upload_session()
            {
                created_by = int.Parse(userId),
                date_created = DateTime.Now,
                org_id = orgId,
                log_date = DateTime.Now.Date,
                status = 1,
                date_last_edited = DateTime.Now                
            };

            try
            {
                using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    upload_session _upload_Session = await c.QuerySingleOrDefaultAsync<upload_session>(@"select * from upload_session us 
                where us.created_by = @created_by and us.org_id = @org_id and log_date = @log_date", upload_Session);

                    if (_upload_Session == null)
                    {
                        upload_Session.id = await c.ExecuteScalarAsync<int>(@"INSERT INTO public.upload_session
                        (org_id, log_date, track_count, status, date_created, date_last_edited, session_name, created_by)                        
                        VALUES(@org_id, @log_date, @track_count, @status, @date_created, @date_last_edited, @session_name, @created_by)
                        RETURNING id;", upload_Session);
                    }
                    else
                    {
                        upload_Session = _upload_Session;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return upload_Session;
        }
    }
}
