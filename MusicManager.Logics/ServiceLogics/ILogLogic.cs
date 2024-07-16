using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface ILogLogic
    {
        void LogErrors((string error, Guid id, string reason)[] errors);
        Task<SyncSummary> GetDailySyncSummaryCount(string orgId,DateTime summaryDate);
    }
}
