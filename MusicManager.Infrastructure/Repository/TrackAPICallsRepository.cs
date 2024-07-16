using Dapper;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Npgsql;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class TrackAPICallsRepository : GenericRepository<log_track_api_calls>, ITrackAPICallsRepository
    {
        private readonly IOptions<AppSettings> _appSettings;
        public TrackAPICallsRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            Context = context;
            _appSettings = appSettings;
        }

        public MLContext Context { get; }

        public async Task<log_track_api_calls> SaveTrackAPICall(log_track_api_calls log_Track_Api_Calls)
        {
            string sql = @"INSERT INTO log.log_track_api_calls(
	        date_created, ws_id, page_size, page_token, response_code, next_page_token,track_count,session_id)
	        VALUES (CURRENT_TIMESTAMP, @ws_id, @page_size, CAST(@page_token AS json), @response_code, CAST(@next_page_token AS json),@track_count,@session_id) returning *;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleAsync<log_track_api_calls>(sql, log_Track_Api_Calls);
            }
        }
    }
}
