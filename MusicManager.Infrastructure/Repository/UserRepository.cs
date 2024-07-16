using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class UserRepository : GenericRepository<org_user>, IUserRepository
    {
        public MLContext _context { get; }
        private readonly IOptions<AppSettings> _appSettings;


        public UserRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _appSettings = appSettings;
            _context = context;
        }

        public async Task<org_user> GetUserById(int userId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<org_user>("select * from org_user ou where ou.user_id = @user_id", new org_user()
                {
                    user_id = userId
                });
            }
        }

        public async Task<string> GetNameById(int userId)
        {
            org_user orgUser = await GetUserById(userId);
            return orgUser == null ? string.Empty : (orgUser.first_name != null ? orgUser.first_name + " " + orgUser.last_name : "");            
        }

        public async Task UpsertUsers(UserPayload userPayload)
        {

            org_user oUser = new org_user()
            {
                email = userPayload.email,
                first_name = userPayload.first_name,
                last_name = userPayload.last_name,
                image_url = userPayload.image_url?.Replace("&amp;", "&"),
                org_id = userPayload.org_id,
                role_id = Convert.ToInt32(userPayload.role_id),
                user_id = Convert.ToInt32(userPayload.user_id),
            };

            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                string query = string.Format("select * from public.org_user where user_id=@user_id;");
                var user = await c.QuerySingleOrDefaultAsync<org_user>(query, oUser);

                if (user == null)
                {
                    string insertQuery = @"INSERT INTO public.org_user(user_id, email, first_name, last_name, org_id, image_url,role_id, date_last_edited)
                                VALUES(@user_id,@email,@first_name, @last_name, @org_id, @image_url, @role_id, now())";

                    var result = await c.ExecuteAsync(insertQuery, oUser);
                    
                }
                else
                {                    
                    oUser.email = userPayload.email;
                    oUser.first_name = userPayload.first_name;
                    oUser.last_name = userPayload.last_name;
                    oUser.image_url = userPayload.image_url?.Replace("&amp;", "&");
                    oUser.org_id = userPayload.org_id;
                    oUser.role_id = Convert.ToInt32(userPayload.role_id);
                    oUser.user_id = Convert.ToInt32(userPayload.user_id);

                    string updateQuery = @"update public.org_user set email = @email,first_name = @first_name,last_name = @last_name,org_id = @org_id,image_url = @image_url, role_id = @role_id, date_last_edited=now() where user_id=@user_id";
                    var result = await c.ExecuteAsync(updateQuery, oUser);
                }

            }
        }
    }    
    
}
