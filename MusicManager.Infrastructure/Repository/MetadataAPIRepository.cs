using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class MetadataAPIRepository : IMetadataAPIRepository
    {        
        private readonly IOptions<AppSettings> _appSettings;       
        private string _token;
        private IMemoryCache _memoryCache;
        private readonly ILogger<MLMasterTrackRepository> _logger;
        private readonly IHttpClientFactory _clientFactory;

        public MetadataAPIRepository(IOptions<AppSettings> appSettings, 
            IMemoryCache memoryCache, 
            ILogger<MLMasterTrackRepository> logger,
            IHttpClientFactory clientFactory)
        {
            _appSettings = appSettings;
            _memoryCache = memoryCache;
            _logger = logger;
            _clientFactory = clientFactory;
        }

        private async Task RequestToken()
        {
            bool isExist = _memoryCache.TryGetValue("TOKEN_METADATA_API", out _token);
            if (isExist)
            {
                _token = _memoryCache.Get<string>("TOKEN_METADATA_API");
                return;
            }

            try
            {
                using (var client = _clientFactory.CreateClient())
                {
                    var apiRequest = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.MetadataApiSettings.API_Endpoint}auth/token");

                    dynamic param = new System.Dynamic.ExpandoObject();
                    param.username = _appSettings.Value.MetadataApiSettings.Username;
                    param.password = _appSettings.Value.MetadataApiSettings.Password;                    

                    apiRequest.Content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(apiRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        dynamic sMToken = JsonConvert.DeserializeObject<dynamic>(responseString);
                        _token = sMToken.token;

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(50));

                        _memoryCache.Set("TOKEN_METADATA_API", _token, cacheEntryOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestToken API failed , Module: {Module}", "Metadata API");
            }
        }        
        
        public async Task<AlbumAPIResponce> GetAlbumListByWSId(string workspaceId, int pageSize, nextPageToken nextPageToken, int retries)
        {
            await RequestToken();
            try
            {
                using (var client = _clientFactory.CreateClient())
                {
                    var apiRequest = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.MetadataApiSettings.API_Endpoint}albums?ws={workspaceId}&pageSize={pageSize}");
                    apiRequest.Headers.Add("Authorization", $"Bearer {_token}");

                    var pageToken = "{\"pageToken\":null}";
                    if (nextPageToken != null)
                        pageToken = $"{{\"pageToken\":{JsonConvert.SerializeObject(nextPageToken, new JsonSerializerSettings())}}}";

                    apiRequest.Content = new StringContent(pageToken, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(apiRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<AlbumAPIResponce>(responseString);
                    }
                    else
                    {
                        if (retries == 0)
                        {
                            _logger.LogError("GetAlbumListByWSId API error code {code} | WorkspaceId: {workspaceId} , Module: {Module}", response?.StatusCode, workspaceId, "Album metadata download - Metadata API");
                        }
                        else
                        {
                            _logger.LogWarning("GetAlbumListByWSId API error code {code}| WorkspaceId: {workspaceId} , Module: {Module}", response?.StatusCode, workspaceId, "Album metadata download - Metadata API");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (retries == 0)
                {
                    _logger.LogError(ex, "GetAlbumListByWSId API failed | WorkspaceId: {workspaceId} , Module: {Module}", workspaceId, "Album metadata download - Metadata API");
                }
                else {
                    _logger.LogWarning(ex, "GetAlbumListByWSId API failed | WorkspaceId: {workspaceId} , Module: {Module}", workspaceId, "Album metadata download - Metadata API");
                }                    
            }

            if (retries == 0)
            {
                return null;
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                return await GetAlbumListByWSId(workspaceId, pageSize, nextPageToken, --retries);
            }                      
        }

        public async Task<TrackAPIResponce> GetTrackListByWSId(string workspaceId, int pageSize, nextPageToken nextPageToken, int retries)
        {
            await RequestToken();
            try
            {
                using (var client = _clientFactory.CreateClient())
                {
                    var apiRequest = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.MetadataApiSettings.API_Endpoint}tracks?ws={workspaceId}&pageSize={pageSize}");
                    apiRequest.Headers.Add("Authorization", $"Bearer {_token}");

                    var pageToken = "{\"pageToken\":null}";
                    if (nextPageToken != null)
                        pageToken = $"{{\"pageToken\":{JsonConvert.SerializeObject(nextPageToken, new JsonSerializerSettings())}}}";

                    apiRequest.Content = new StringContent(pageToken, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(apiRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<TrackAPIResponce>(responseString);
                    }
                    else
                    {
                        if (retries == 0)
                        {
                            _logger.LogError("GetTrackListByWSId API error code {code} | WorkspaceId: {workspaceId} , Module: {Module}", response?.StatusCode, workspaceId, "Track metadata download - Metadata API");
                        }
                        else
                        {
                            _logger.LogWarning("GetTrackListByWSId API error code {code}| WorkspaceId: {workspaceId} , Module: {Module}", response?.StatusCode, workspaceId, "Track metadata download - Metadata API");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (retries == 0) {
                    _logger.LogError(ex, "GetTrackListByWSId API failed | WorkspaceId: {workspaceId} , Module: {Module}", workspaceId, "Track metadata download - Metadata API");
                }
                else
                {
                    _logger.LogWarning(ex, "GetTrackListByWSId API failed | WorkspaceId: {workspaceId} , Module: {Module}", workspaceId, "Track metadata download - Metadata API");
                }
            }

            if (retries == 0)
            {
                return null;
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                return await GetTrackListByWSId(workspaceId, pageSize, nextPageToken, --retries);
            }
        }

        public async Task<HttpWebResponse> httpPostRequest(string url, byte[] data)
        {
            HttpWebRequest httpWebRequest;
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();

            try
            {
                httpWebRequest = WebRequest.Create(_appSettings.Value.MetadataApiSettings.API_Endpoint + url) as HttpWebRequest;
                httpWebRequest.Headers.Add(webHeaderCollection);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + _token);

                using (var stream = httpWebRequest.GetRequestStream())
                {
                    if (data != null)
                        stream.Write(data, 0, data.Length);
                }

                return await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "httpPostRequest > URL - " + url);
                return null;
            }
        }

        public async Task<List<MetadataLibrary>> GetAllLibraries(int retries = 2)
        {
            try
            {
                await RequestToken();
                string _url = _appSettings.Value.MetadataApiSettings.API_Endpoint + "libraries";                

                HttpWebResponse httpResponse = await httpGetRequest(_url, null);

                if (httpResponse?.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(httpResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        string output = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<List<MetadataLibrary>>(output);
                    }
                }
                else {
                    if (retries > 1)
                    {
                        _logger.LogWarning("GetAllLibraries Metadata API error code: {ErrorCode} | Retry attempt: {Retry} | Module: {Module}", httpResponse?.StatusCode, retries, "Libarary Sync");
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                        await GetAllWorkspaces(retries - 1);
                    }
                    _logger.LogError("GetAllLibraries Metadata API error code: {ErrorCode} | Module: {Module}", httpResponse?.StatusCode, "Libarary Sync");
                    return null;
                }
                
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "GetAllLibraries Metadata API failed | Retry attempt: {Retry} | Module: {Module}", retries, "Libarary Sync");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    await GetAllWorkspaces(retries - 1);
                }
                _logger.LogError(ex, "GetAllLibraries Metadata API failed | Module: {Module}", "Libarary Sync");
                return null;               
            }
        }

        public async Task<List<MetadataWorkspace>> GetAllWorkspaces(int retries = 2)
        {
            try
            {
                await RequestToken();

                string _url = _appSettings.Value.MetadataApiSettings.API_Endpoint + "workspaces";

                HttpWebResponse httpResponse = await httpGetRequest(_url, null);

                if (httpResponse?.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(httpResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        string output = reader.ReadToEnd();
                        return JsonConvert.DeserializeObject<List<MetadataWorkspace>>(output);                      
                    }
                }
                else
                {
                    if (retries > 1)
                    {
                        _logger.LogWarning("GetAllWorkspaces Metadata API error code: {ErrorCode} | Retry attempt: {Retry} | Module: {Module}", httpResponse?.StatusCode, retries, "Workspace Sync");
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                        await GetAllWorkspaces(retries - 1);
                    }
                    _logger.LogError("GetAllWorkspaces Metadata API error code: {ErrorCode} | Module: {Module}", httpResponse?.StatusCode, "Workspace Sync");
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "GetAllWorkspaces Metadata API failed | Retry attempt: {Retry} | Module: {Module}", retries, "Workspace Sync");
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    await GetAllWorkspaces(retries - 1);
                }
                _logger.LogError(ex, "GetAllWorkspaces Metadata API failed | Module: {Module}", "Workspace Sync");
                return null;
            }
        }      

        private async Task<HttpWebResponse> httpGetRequest(string url, byte[] data)
        {
            HttpWebRequest httpWebRequest;
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();

            try
            {
                httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Headers.Add(webHeaderCollection);
                httpWebRequest.Method = "GET";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + _token);                

                return await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException)
            {
                throw;
            }
        }        
        
    }
}
