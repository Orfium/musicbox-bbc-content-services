using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IActionLoggerLogic
    {
        Task LogAction(ActivityLog activityLog);
        Task ServiceLog(ServiceLog serviceLog, int retries = 2);
        Task<bool> LogUserAction(UserAction userAction);
    }
}
