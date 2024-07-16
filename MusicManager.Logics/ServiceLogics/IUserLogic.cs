using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IUserLogic
    {
        Task UpsertUsers(UserPayload userPayload);
    }
}
