using Microsoft.Extensions.Configuration;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IAuthentication
    {
        Task<string> AuthenticateUser(UserModel userModel);
    }
}
