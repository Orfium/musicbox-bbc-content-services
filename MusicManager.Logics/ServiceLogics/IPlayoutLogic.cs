using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IPlayoutLogic
    {
        Task<PlayoutResponse> CreatePlayOut(PlayoutPayload playOutSession, enPlayoutAction action);
        Task<int> DeletePlayOutTracks(PlayoutTrackIdPayload tracks);       
        Task<int> AddTracksToPlayOut(AddPlayoutPayload playOutSession);
        Task<byte[]> DownloadPlayoutXML_ZIP(PlayoutXMlDownloadPayload playoutXMlDownloadPayload);
        Task<InMemoryFile> DownloadPlayoutXML(PlayoutXMlDownloadPayload playoutXMlDownloadPayload);
        Task ProcessPublishPlayOut();
        Task S3Cleanup();
        Task UpdateSigniantReplay(Guid requestId,Playout.Models.Signiant.SigniantReplyResponse signiantReplyResponse);
        Task UpdateSigniantFault(Guid requestId,Playout.Models.Signiant.SigniantFaultResponse signiantFaultResponse);
        Task<int> RestartPlayout(int plaoutId);
    }
}
