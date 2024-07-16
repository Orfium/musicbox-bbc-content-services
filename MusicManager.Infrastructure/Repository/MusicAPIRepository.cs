using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Extensions;
using MusicManager.Logics.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class MusicAPIRepository : IMusicAPIRepository
    {
        private readonly IOptions<AppSettings> _appSettings;
        private IMemoryCache _memoryCache;
        private readonly ILogger<MusicAPIRepository> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private string _token;

        public MusicAPIRepository(IOptions<AppSettings> appSettings, 
            IMemoryCache memoryCache, 
            ILogger<MusicAPIRepository> logger,
            IHttpClientFactory clientFactory)
        {
            _appSettings = appSettings;
            _memoryCache = memoryCache;
            _logger = logger;
            _clientFactory = clientFactory;
        }

        private async Task RequestToken()
        {
            bool isExist = _memoryCache.TryGetValue("TOKEN_MUSIC_API", out _token);
            if (isExist)
            {
                _token = _memoryCache.Get<string>("TOKEN_MUSIC_API");
                return;
            }

            try
            {
                using (var client = _clientFactory.CreateClient())
                {
                    var apiRequest = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.MusicApiSettings.API_Endpoint}auth/token");

                    dynamic param = new System.Dynamic.ExpandoObject();
                    param.username = _appSettings.Value.MusicApiSettings.Username;
                    param.password = _appSettings.Value.MusicApiSettings.Password;

                    apiRequest.Content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(apiRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();

                        dynamic sMToken = JsonConvert.DeserializeObject<dynamic>(responseString);
                        _token = sMToken.token;

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(50));

                        _memoryCache.Set("TOKEN_MUSIC_API", _token, cacheEntryOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestToken API failed , Module: {Module}", "Music API");
            }
        }        

        private async Task<HttpWebResponse> httpPOSTRequest(string url, dynamic requestPayload)
        {
            await RequestToken();  

            HttpWebRequest httpWebRequest;
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
            string serializeObject = string.Empty; 

            try
            {
                httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Headers.Add(webHeaderCollection);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + _token);                

                if (requestPayload != null)
                {
                    serializeObject = JsonConvert.SerializeObject(requestPayload, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    byte[] data = Encoding.UTF8.GetBytes(serializeObject);

                    using (var stream = httpWebRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                return await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "httpPOSTRequest > URL : " + url + " / object : " + serializeObject);
                return (HttpWebResponse)ex.Response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "httpPOSTRequest > URL : " + url + " / object : " + serializeObject);                
                return null;
            }
        }

        private async Task<HttpWebResponse> httpPUTRequest(string url, dynamic requestPayload)
        {
            await RequestToken();

            HttpWebRequest httpWebRequest;
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
            string serializeObject = JsonConvert.SerializeObject(requestPayload, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            try
            {
                httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Headers.Add(webHeaderCollection);
                httpWebRequest.Method = "PUT";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + _token);

                if (requestPayload != null)
                {
                    byte[] data = Encoding.UTF8.GetBytes(serializeObject);

                    using (var stream = httpWebRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                var x = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
                return x;
            }
            catch (WebException ex)
            {
                _logger.LogError(ex, "httpPUTRequest > URL : " + url + " / object : " + serializeObject);
                return (HttpWebResponse)ex.Response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "httpPUTRequest > URL : " + url + " / object : " + serializeObject);               
                return null;
            }
        }

        private async Task<HttpWebResponse> httpDeleteRequest(string url)
        {
            await RequestToken();

            HttpWebRequest httpWebRequest;
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();

            try
            {
                httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Headers.Add(webHeaderCollection);
                httpWebRequest.Method = "DELETE";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Headers.Add("Authorization", "Bearer " + _token);               

                return await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        if ((int)response.StatusCode != 404)
                        {
                            _logger.LogError(ex, "httpDeleteRequest > " + url);
                        }                        
                    }                    
                }  
                
                return (HttpWebResponse)ex.Response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "httpDeleteRequest > " + url);
                return null;
            }
        }

        private async Task<HttpWebResponse> httpGetRequest(string url)
        {
            await RequestToken();

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
            catch (WebException ex)
            {
                _logger.LogError(ex, "httpDeleteRequest > " + url);
                return (HttpWebResponse)ex.Response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "httpDeleteRequest > " + url);
                return null;
            }
        }

        private async Task<HttpStatusCode> PutAsset(string requestURL, byte[] fileStream)
        {
            try
            {
                await RequestToken();

                HttpWebRequest _httpWebRequest = WebRequest.Create(requestURL) as HttpWebRequest;
                _httpWebRequest.Method = "PUT";
                _httpWebRequest.ContentType = "audio/wave";
                _httpWebRequest.Headers.Add("Authorization", "Bearer " + _token);
                _httpWebRequest.ContentLength = fileStream.Length;
                _httpWebRequest.AllowWriteStreamBuffering = true;

                _httpWebRequest.ReadWriteTimeout = 1200000;
                _httpWebRequest.Timeout = 1200000;

                Stream stream = await _httpWebRequest.GetRequestStreamAsync();
                stream.Write(fileStream, 0, fileStream.Length);

                dynamic response = await _httpWebRequest.GetResponseAsync();

                return response.StatusCode;
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = ex.Response as HttpWebResponse;
                    if (response != null)
                    {
                        if ((int)response.StatusCode != 404)
                        {
                            _logger.LogError(ex, "PutAsset > URL : " + requestURL);
                        }
                    }
                }
                return HttpStatusCode.BadRequest;
            }
        }

        private async Task<HttpStatusCode> PutArtwork(string requestURL, byte[] fileStream)
        {
            try
            {
                await RequestToken();

                HttpWebRequest _httpWebRequest = WebRequest.Create(requestURL) as HttpWebRequest;
                _httpWebRequest.Method = "PUT";
                _httpWebRequest.Headers.Add("Authorization", "Bearer " + _token);
                _httpWebRequest.ContentLength = fileStream.Length;
                _httpWebRequest.AllowWriteStreamBuffering = true;

                _httpWebRequest.ReadWriteTimeout = 1200000;
                _httpWebRequest.Timeout = 1200000;

                Stream stream = _httpWebRequest.GetRequestStream();

                stream.Write(fileStream, 0, fileStream.Length);

                stream.Close();

                dynamic response = await _httpWebRequest.GetResponseAsync();

                return response.StatusCode;
            }
            catch (WebException)
            {
                throw;
            }
        }

        public async Task<DHTrack> GetTrackById(string trackId)
        {
            DHTrack dHTrack = null;
            try
            {                
                await RequestToken();
                string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks/{trackId}";
                HttpWebResponse httpWebResponse = await httpGetRequest(_url);

                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        string output = reader.ReadToEnd();
                        dHTrack = JsonConvert.DeserializeObject<DHTrack>(output);
                    }
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTrackById > ID : " + trackId);
            }
            return dHTrack;
        }  
        

        public async Task<HttpWebResponse> DeleteTrack(string trackId)
        {
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks/{trackId}";
            return await httpDeleteRequest(_url);
        }

        public async Task<HttpWebResponse> DeleteAlbum(string albumId)
        {           
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums/{albumId}";
            return await httpDeleteRequest(_url);
        }

        public async Task<HttpWebResponse> CreateTrack(string workspaceId, dynamic DHTrack)
        {
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks?ws={workspaceId}";
            return await httpPOSTRequest(_url, DHTrack);
        }

        public async Task<HttpWebResponse> PostAlbum(string workspaceId, dynamic mA_Track)
        {            
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums?ws={workspaceId}";
            return await httpPOSTRequest(_url, mA_Track);
        }

        public async Task<DHTrack> UpdateTrack(string trackId, dynamic dhTrack)
        {
            DHTrack dHTrackResponce = null;            
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks/{trackId}";
            HttpWebResponse httpWebResponse = await httpPUTRequest(_url, dhTrack);

            if (httpWebResponse?.StatusCode == HttpStatusCode.OK)
            {
                using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    string result = streamReader.ReadToEnd();
                    dHTrackResponce = JsonConvert.DeserializeObject<DHTrack>(result);
                }
            }
            return dHTrackResponce;
        }

        public async Task<HttpWebResponse> SendImportBegin(List<MA_BulkUploadPayload> bulkUploadPayloads)
        {           
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks/import/begin";
            return await httpPOSTRequest(_url, bulkUploadPayloads);
        }

        public async Task<HttpWebResponse> CheckImportStatus(List<string> trackIds)
        {            
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks/import/status";
            return await httpPOSTRequest(_url, trackIds);
        }

        public async Task<HttpStatusCode> UploadTrack(string trackId, byte[] fileStream)
        {   
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks/{trackId}/audio";
            return await PutAsset(_url, fileStream);
        }

        public async Task<HttpWebResponse> GetTrackByUniqueId(string workspaceId, string uniqueId)
        {
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks?ws={workspaceId}&q=uniqueId:{uniqueId}";
            return await httpGetRequest(_url);

        }

        public async Task<HttpWebResponse> GetAlbumByUniqueId(string workspaceId, string uniqueId)
        {
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums?ws={workspaceId}&q=uniqueId:{uniqueId}";
            return await httpGetRequest(_url);
        }

        public async Task<DHTrack> CreateUploadTrack(string workspaceId, upload_track track, Guid? albumId, org_user orgUser)
        {
            DHTrack dHTrack = null;
            if (track.metadata_json != null)
            {

                EditTrackMetadata mLTrackMetadataEdit = JsonConvert.DeserializeObject<EditTrackMetadata>(track.metadata_json);

                UploadDesctiptiveRef uploadDesctiptiveRef = new UploadDesctiptiveRef() {
                    Action = enTrackChangeLogAction.UPLOAD.ToString(),
                    UserId = orgUser.user_id,
                    DateCreated = DateTime.Now,
                    UserName = orgUser.first_name !=null ? orgUser.first_name + " " + orgUser.last_name : "",
                    RefId = track.upload_id,
                    AssetS3Id = _appSettings.Value.AWSS3.FolderName + "/"  + track.s3_id,
                    BucketName = _appSettings.Value.AWSS3.BucketName,
                    Size = track.size
                };

                if (mLTrackMetadataEdit != null)
                {
                    dHTrack = DHTrackEditExtention.CreateDHTrackFromEditTrackMetadata(null, mLTrackMetadataEdit, track.id.ToString());                                        

                    dHTrack.descriptiveExtended = new List<Soundmouse.Messaging.Model.DescriptiveData>();
                    dHTrack.descriptiveExtended.Add(new Soundmouse.Messaging.Model.DescriptiveData()
                    {
                        DateExtracted = DateTime.Now,
                        Source = enDescriptiveExtendedSource.ML_UPLOAD.ToString(),
                        Type = enDescriptiveExtendedType.upload_track_id.ToString(),
                        Value = uploadDesctiptiveRef                        
                    });

                    if (albumId != null)
                        dHTrack.albumId = albumId;
                }
            }

            if (dHTrack != null)
            {
                dHTrack = await CreateDHTrack(workspaceId, dHTrack);
            }
            return dHTrack;
        }

        public async Task<DHTrack> CreateDHTrack(string workspaceId, DHTrack dHTrack)
        {
            DHTrack dHTrackResponce = null;

            HttpWebResponse httpWebResponse = await CreateTrack(workspaceId, dHTrack);            

            if (httpWebResponse != null)
            {
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        string result = streamReader.ReadToEnd();
                        dHTrackResponce = JsonConvert.DeserializeObject<DHTrack>(result);
                    }
                }               
            }
            return dHTrackResponce;
        }

        public async Task<DHAlbum> CreateAlbum(string workspaceId, DHAlbum dHAlbum)
        {
            DHAlbum dHAlbumResponce = null;

            HttpWebResponse httpWebResponse;

            if (dHAlbum != null)
            {
                httpWebResponse = await PostAlbum(workspaceId, dHAlbum);
                if (httpWebResponse?.StatusCode == HttpStatusCode.OK)
                {
                    using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                    {
                        string result = streamReader.ReadToEnd();
                        dHAlbumResponce = JsonConvert.DeserializeObject<DHAlbum>(result);
                    }
                }
            }
            return dHAlbumResponce;
        }

        public async Task<DHAlbum> PrepareAndCreateAlbum(string workspaceId, upload_album upload_Album, org_user orgUser)
        {
            DHAlbum dHAlbumResponce = null;            
            DHAlbum dHAlbum = null;

            if (!string.IsNullOrWhiteSpace(upload_Album.metadata_json))
            {
                EditAlbumMetadata editAlbumMetadata = JsonConvert.DeserializeObject<EditAlbumMetadata>(upload_Album.metadata_json);
                dHAlbum = editAlbumMetadata.CreateDHAlbumFromEditAlbumMetadata((Guid)upload_Album.upload_id, orgUser);
            }

            if (dHAlbum != null)
            {
                dHAlbum.id = upload_Album.dh_album_id;
                dHAlbumResponce = await CreateAlbum(workspaceId, dHAlbum);
            }
            return dHAlbumResponce;
        }

        public async Task<HttpStatusCode> UploadArtwork(string albumId, byte[] fileStream)
        {           
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums/{albumId}/artwork";
            return await PutArtwork(_url, fileStream);
        }

        public async Task<DHAlbum> UpdateAlbum(string albumId, dynamic albumData)
        {
            DHAlbum dHAlbum = null;
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums/{albumId}";
            HttpWebResponse httpWebResponse = await httpPUTRequest(_url, albumData);

            if (httpWebResponse?.StatusCode == HttpStatusCode.OK)
            {
                using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    string result = streamReader.ReadToEnd();
                    dHAlbum = JsonConvert.DeserializeObject<DHAlbum>(result);
                }
            }
            return dHAlbum;
        }

        public async Task<HttpWebResponse> DHAssetCopy(Guid trackId, string assetURL)
        {
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}tracks/{trackId}/audio?download_url={assetURL}";
            return await httpPOSTRequest(_url, null);
        }

        public async Task<string> GetAlbumArtwork(Guid albumId)
        {           
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums/{albumId}/artwork/url";
            HttpWebResponse httpWebResponse = await httpGetRequest(_url);

            if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var reader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
                {
                    string output = reader.ReadToEnd();
                    var asset =  JsonConvert.DeserializeObject<dynamic>(output);
                    if(asset != null)
                    {
                        string url = Convert.ToString(asset.url);
                        return url.Split('?')[0];
                    }
                }
            }
            return null;
        }       

        public async Task CopyArtwork(Guid albumId, string artworkUrl)
        {           
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums/{albumId}/artwork?download_url={artworkUrl}";
            HttpWebResponse httpWebResponse = await httpPOSTRequest(_url, null);
        }        

        public async Task<DHAlbum> GetAlbumById(Guid albumId)
        {
            DHAlbum dHAlbum = null;
            string _url = $"{_appSettings.Value.MusicApiSettings.API_Endpoint}albums/{albumId}";
            HttpWebResponse httpWebResponse = await httpGetRequest(_url);

            if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var reader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
                {
                    string output = reader.ReadToEnd();
                    dHAlbum = JsonConvert.DeserializeObject<DHAlbum>(output);                   
                }
            }
            return dHAlbum;
        }
    }

}
