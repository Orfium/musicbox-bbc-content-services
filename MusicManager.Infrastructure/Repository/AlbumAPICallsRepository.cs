using Dapper;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Npgsql;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class AlbumAPICallsRepository : GenericRepository<log_album_api_calls>, IAlbumAPICallsRepository
    {
        private readonly IOptions<AppSettings> _appSettings;
        public AlbumAPICallsRepository(MLContext context, IOptions<AppSettings> appSettings) : base(context)
        {
            Context = context;
            _appSettings = appSettings;
        }

        public MLContext Context { get; }

        public async Task<log_album_api_calls> SaveAlbumAPICall(log_album_api_calls log_Album_Api_Calls)
        {
            string sql = @"INSERT INTO log.log_album_api_calls(
	        date_created, ws_id, page_size, page_token, response_code, next_page_token,album_count,session_id)
	        VALUES (CURRENT_TIMESTAMP, @ws_id, @page_size, CAST(@page_token AS json), @response_code, CAST(@next_page_token AS json),@album_count,@session_id) returning *;";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleAsync<log_album_api_calls>(sql, log_Album_Api_Calls);
            }
        }
    }
}
