using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Npgsql;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class MemberLabelRepository : GenericRepository<member_label>, IMemberLabelRepository
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<MemberLabelRepository> _logger;

        public MemberLabelRepository(MLContext context, IOptions<AppSettings> appSettings,
            ILogger<MemberLabelRepository> logger) : base(context)
        {          
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task InsertManualLabel(member_label label)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    await c.ExecuteAsync(@"INSERT INTO public.member_label(member,label, source,mlc, date_created,created_by)
                VALUES (@member, @label, @source,@mlc,CURRENT_TIMESTAMP,@created_by);", label);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Insert PPL Label");
            }
        }

        public async Task UpdateLabel(member_label label)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    await c.ExecuteAsync(@"update public.member_label set member=@member,label=@label, source=@source,mlc=@mlc,
                    date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by 
                    where id=@id;", label);
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Update PPL Label");
            }
        }
    }
}
