using Dapper;
using Microsoft.Extensions.Options;
using MusicManager.UserSync.ViewModels;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.UserSync
{

    public class UserSync
    {
        private static IOptions<AppSettings> _appSettings;

        public UserSync(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        public static async Task SyncOrgUsers()
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.SyncConnectionString))
                {
                    string query = "select ou.id,ou.email,ou.\"firstName\",ou.\"lastName\",ou.\"orgId\",ou.\"imageUrl\",ou.roleid from public.\"orgUsers\" ou";
                    var userList = await c.QueryAsync<UserViewModel>(query);

                    foreach (var user in userList)
                    {
                        try
                        {
                            await UpsertUsers(user);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error - ", ex);
                        }
                        
                    }

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
 
        }

        private static async Task UpsertUsers(UserViewModel userViewModel)
        {
            
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {

                SyncUserViewModel user = await c.QuerySingleOrDefaultAsync<SyncUserViewModel>("select * from public.org_user where user_id=@user_id", new { user_id = userViewModel.id });

                if (user == null)
                {
                    string q =string.Format(@"INSERT INTO public.org_user(user_id, email, first_name, last_name, org_id, image_url,role_id,date_last_edited)
                                VALUES({0},'{1}','{2}', '{3}', '{4}', '{5}', {6},CURRENT_TIMESTAMP)",
                                userViewModel.id , userViewModel.email , userViewModel.firstName ,
                                 userViewModel.lastName , userViewModel.orgId , userViewModel.imageUrl , userViewModel.roleid);
                                    
                    var result = await c.ExecuteAsync(q);
                    if (result == 1)
                    {
                        Console.WriteLine("Insert Success for User: {0}", userViewModel.id);
                    }
                    else
                    {
                        Console.WriteLine("Insert UnSuccess for User: {0}", userViewModel.id);
                    }
                }
                else
                {
                    StringBuilder _SB = new StringBuilder();
                    _SB.Append("update public.org_user ");
                    _SB.AppendFormat("set email = '{0}',first_name = '{1}',last_name = '{2}',org_id = '{3}',image_url = '{4}', role_id = {5},date_last_edited=CURRENT_TIMESTAMP ", 
                        userViewModel.email, userViewModel.firstName, userViewModel.lastName, userViewModel.orgId, userViewModel.imageUrl, userViewModel.roleid);
                    _SB.AppendFormat("where user_id='{0}'", userViewModel.id);

                    var result = await c.ExecuteAsync(_SB.ToString());
                    if (result == 1)
                    {
                        Console.WriteLine("Update Success for User: {0}", userViewModel.id);
                    }
                    else
                    {
                        Console.WriteLine("Update UnSuccess for User: {0}", userViewModel.id);
                    }
                }

            }
        }
    }
}
