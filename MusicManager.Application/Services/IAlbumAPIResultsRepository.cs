using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IAlbumAPIResultsRepository : IGenericRepository<log_album_api_results>
    {
        Task<int> BulkInsert(List<log_album_api_results> logAlbumApiResults);
        Task<int> Insert(log_album_api_results log_Album_Api_Results);       
        Task DeleteAllBySessionId(int sessionId);
    }
}
