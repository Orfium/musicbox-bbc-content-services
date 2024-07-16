using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IAlbumAPICallsRepository : IGenericRepository<log_album_api_calls>
    {
        Task<log_album_api_calls> SaveAlbumAPICall(log_album_api_calls log_Album_Api_Calls);
    }
}
