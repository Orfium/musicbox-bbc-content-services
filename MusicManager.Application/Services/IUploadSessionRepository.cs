using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IUploadSessionRepository : IGenericRepository<upload_session>
    {
        Task<upload_session> CreateSession(string userId, string orgId);
    }
}
