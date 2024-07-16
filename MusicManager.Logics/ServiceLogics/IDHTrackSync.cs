using MusicManager.Application;
using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IDHTrackSync
    {
        Task<int> DownloadDHTracks(workspace workspace, enServiceType enServiceType);
        Task<int> DownloadDHAlbums(workspace workspace, enServiceType enServiceType);
        Task<int> ElasticIndex(workspace_org workspace_org, enServiceType enServiceType);
        Task<int> AlbumElasticIndex(workspace_org workspace_org, enServiceType enServiceType);       
        Task SyncTracks(workspace_org workspace_Org, enServiceType enServiceType);
    }
}
