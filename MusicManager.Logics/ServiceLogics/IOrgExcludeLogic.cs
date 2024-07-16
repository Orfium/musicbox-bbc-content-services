using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IOrgExcludeLogic
    {
        Task<int> OrgExclude(SyncActionPayload syncActionPayload);
    }
}
