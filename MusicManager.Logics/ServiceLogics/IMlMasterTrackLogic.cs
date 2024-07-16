using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IMlMasterTrackLogic
    {
        Task<EditTrackMetadata> MakeMLCopy(MLCopyPayload mLCopyPayload);
    }
}
