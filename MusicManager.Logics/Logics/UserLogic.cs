using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class UserLogic: IUserLogic
    {
        private readonly IUnitOfWork _unitOfWork;
        public UserLogic( IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task UpsertUsers(UserPayload userPayload)
        {
            await _unitOfWork.User.UpsertUsers(userPayload);
        }
       
    }
}
