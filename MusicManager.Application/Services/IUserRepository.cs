using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IUserRepository : IGenericRepository<org_user>
    {
        Task<org_user> GetUserById(int userId);
        Task<string> GetNameById(int userId);
        Task UpsertUsers(UserPayload userPayload);
    }
}
