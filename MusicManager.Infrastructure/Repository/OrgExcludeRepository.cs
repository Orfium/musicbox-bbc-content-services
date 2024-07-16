using Dapper;
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
    public class OrgExcludeRepository: GenericRepository<org_exclude>, IOrgExcludeRepository
    {
        public MLContext _context { get; }
        public IOptions<AppSettings> _appSettings { get; }

        public OrgExcludeRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
        }

        public async Task<org_exclude> GetByRefIdAndType(Guid refId, string type)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<org_exclude>(@"select * from org_exclude oe where oe.ref_id=@ref_id and oe.item_type=@item_type;",
                                                                            new org_exclude()
                                                                            {
                                                                                ref_id= refId,
                                                                                item_type = type
                                                                            });

            }
        }

        public Task<int> Save(org_exclude org_Exclude)
        {
            throw new NotImplementedException();
        }

        public Task<int> Delete(org_exclude org_Exclude)
        {
            throw new NotImplementedException();
        }
    }
}
