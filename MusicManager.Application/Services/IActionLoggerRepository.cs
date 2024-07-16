using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IActionLoggerRepository : IGenericRepository<log_user_action>
    {
        Task Log(log_user_action c_tag);        
    }
}
