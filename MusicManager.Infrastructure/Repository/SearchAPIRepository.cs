using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class SearchAPIRepository : ISearchAPIRepository
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<MLMasterTrackRepository> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private string _token;

        public SearchAPIRepository(IOptions<AppSettings> appSettings,
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
            bool isExist = _memoryCache.TryGetValue("TOKEN_SEARCH_API", out _token);
            if (isExist)
            {
                _token = _memoryCache.Get<string>("TOKEN_SEARCH_API");
                return;
            }

            try
            {
                using (var client = _clientFactory.CreateClient())
                {
                    var apiRequest = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.SMSearchApiSettings.API_Endpoint}login");

                    dynamic param = new System.Dynamic.ExpandoObject();
                    param.username = _appSettings.Value.SMSearchApiSettings.Username;
                    param.password = _appSettings.Value.SMSearchApiSettings.Password;

                    apiRequest.Content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(apiRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        dynamic sMToken = JsonConvert.DeserializeObject<dynamic>(responseString);

                        _token = sMToken;                       

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(50));

                        _memoryCache.Set("TOKEN_SEARCH_API", _token, cacheEntryOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RequestToken API failed , Module: {Module}", "SM Search API");
            }
        }              

        public async Task<List<Guid>> CheckDeletedTracks(List<Guid> guids)
        {
            try
            {
                var _query = new { query = new { or = new List<object>() }, page = new { from = 0, size = guids.Count } };

                foreach (var item in guids)
                {
                    _query.query.or.Add(new { exact = new { id = item } });
                }

                await RequestToken();
                using (var httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip })
                {
                    BaseAddress = new Uri(_appSettings.Value.SMSearchApiSettings.API_Endpoint),
                    Timeout = TimeSpan.FromSeconds(100)
                })
                using (var content = new StringContent(JsonConvert.SerializeObject(_query), Encoding.UTF8, "application/json"))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                    var response = await httpClient.PostAsync("search", content);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;                       

                        var dynResult = JsonConvert.DeserializeObject<SearchAPIResponse>(result);
                        if (dynResult.results.Count() > 0)
                        {
                            foreach (var item in dynResult.results)
                            {
                                if (!string.IsNullOrEmpty(item.originalUrl))
                                    guids.Remove(item.id);
                            }
                        }
                        return guids;
                    }
                    else {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckDeletedTracks on search API");
                return null;
            }
        }
    }
}
