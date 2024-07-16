using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IAlbumSyncSessionRepository : IGenericRepository<log_album_sync_session>
    {
        Task<log_album_sync_session> SaveAlbumSyncSession(log_album_sync_session log_Album_Sync_Session);
        Task<int> UpdateAlbumSyncSession(log_album_sync_session log_Album_Sync_Session);
    }
}
