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
    public class TunecodeIsrcRepository : GenericRepository<isrc_tunecode>, ITunecodeIsrcRepository
    {
        public IOptions<AppSettings> _appSettings { get; }

        public TunecodeIsrcRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _appSettings = appSettings;

        }

        public async Task<int> AddIsrcTunecode(string isrc, string tunecode)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    string q = (@"INSERT INTO staging.isrc_tunecode(isrc, tunecode)
                                    VALUES(@isrc, @tunecode)"
                                  );
                    return await c.ExecuteAsync(q, new isrc_tunecode() { isrc = isrc, tunecode = tunecode });
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public async Task<bool> IsExist(string isrc, string tunecode)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteScalarAsync<bool>("select 1 from staging.isrc_tunecode it where it.isrc=@isrc and it.tunecode=@tunecode",
                        new isrc_tunecode() { isrc = isrc, tunecode = tunecode });
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

    }
}
