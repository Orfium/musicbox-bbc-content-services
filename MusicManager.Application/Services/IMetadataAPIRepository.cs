using MusicManager.Core.ViewModules;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IMetadataAPIRepository
    {
        Task<HttpWebResponse> httpPostRequest(string url, byte[] data);
        Task<List<MetadataLibrary>> GetAllLibraries(int retries = 2);
        Task<List<MetadataWorkspace>> GetAllWorkspaces(int retries = 2);
        Task<TrackAPIResponce> GetTrackListByWSId(string worspaceid, int pageSize, nextPageToken nextPageToken, int retries = 2);
        Task<AlbumAPIResponce> GetAlbumListByWSId(string worspaceid, int pageSize, nextPageToken nextPageToken, int retries = 2);       
    }
}
