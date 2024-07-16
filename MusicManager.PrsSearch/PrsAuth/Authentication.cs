using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TokenServiceReference;


namespace MusicManager.PrsSearch.PrsAuth
{
    public class Authentication: IAuthentication
    {
        private TokenServiceSoapClient _client;
        private readonly IMemoryCache _memoryCache;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<Authentication> _logger;

        public Authentication(IMemoryCache memoryCache,
            IOptions<AppSettings> appSettings,
            ILogger<Authentication> logger)
        {
            _memoryCache = memoryCache;
            _appSettings = appSettings;
            _logger = logger;
            _client = new TokenServiceSoapClient(TokenServiceSoapClient.EndpointConfiguration.TokenServiceSoap);
        }

        public string GetSessionToken(bool checkCache = true)
        {
            string prsSessionToken = string.Empty;
            try
            {
                bool isExist = _memoryCache.TryGetValue("TOKEN_PRS_API", out prsSessionToken);
                if (isExist && checkCache == true)
                {
                    prsSessionToken = _memoryCache.Get<string>("TOKEN_PRS_API");
                }
                else
                {
                    prsSessionToken = RequestSessionId();

                    if (!string.IsNullOrEmpty(prsSessionToken))
                    {
                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                           .SetAbsoluteExpiration(TimeSpan.FromMinutes(40));

                        _memoryCache.Set("TOKEN_PRS_API", prsSessionToken, cacheEntryOptions);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSessionToken | Module: {Module}", "PRS Search");
            }

            return prsSessionToken;
        }

        private string RequestSessionId(int retries = 3)
        {
            try
            {
                _logger.LogInformation("Request PRS Session Id | Module: {Module}", "PRS Search");
                var newToken = _client.LoginAsync(_appSettings.Value.PRSSettings.Username, _appSettings.Value.PRSSettings.Password).Result;
                return newToken.Body.LoginResult;
            }
            catch (Exception ex)
            {
                _client = new TokenServiceSoapClient(TokenServiceSoapClient.EndpointConfiguration.TokenServiceSoap);

                if (retries > 1)
                {
                    _logger.LogWarning(ex, "Error retrieving session ID from PRS Service | Retry: {Retry} | Module: {Module}", retries, "PRS Search");
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    RequestSessionId(retries - 1);
                }
                _logger.LogError(ex, "Error retrieving session ID from PRS Service. | Module: {Module}", "PRS Search");
                return null;
            }
        }
    }
}
