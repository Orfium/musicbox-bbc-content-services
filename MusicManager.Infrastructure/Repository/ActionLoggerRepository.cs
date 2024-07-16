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
    public class ActionLoggerRepository : GenericRepository<log_user_action>, IActionLoggerRepository
    {      
        public IOptions<AppSettings> _appSettings { get; }

        public ActionLoggerRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {           
            _appSettings = appSettings;
        }

        public async Task Log(log_user_action c_tag)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    string q = (@"INSERT INTO log.log_user_action(action_id, user_id, date_created, org_id, old_value, new_value, data_type, ref_id, data_value, status, exception)
                                    VALUES(@action_id, @user_id, @date_created, @org_id, @old_value, @new_value, @data_type, @ref_id, @data_value, @status, @exception)"
                                  );
                    await c.ExecuteAsync(q, c_tag);

                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
    }
}
