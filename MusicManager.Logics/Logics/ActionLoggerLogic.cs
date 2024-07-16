using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Helper;
using MusicManager.Logics.ServiceLogics;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class ActionLoggerLogic : IActionLoggerLogic
    {
        private readonly IElasticClient _elasticClient;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<ActionLoggerLogic> _logger;
        private List<ServiceLogTime> _serviceLogTime;

        public ActionLoggerLogic(IElasticClient elasticClient,
            IOptions<AppSettings> appSettings, ILogger<ActionLoggerLogic> logger)
        {
            _elasticClient = elasticClient;
            _appSettings = appSettings;
            _logger = logger;
            _serviceLogTime = new List<ServiceLogTime>();
        }

      
        public async Task<bool> LogUserAction(UserAction userAction)
        {
            IndexResponse indexResponse = await _elasticClient.IndexAsync(new IndexRequest<UserAction>(userAction, $"medialibrary-content-user-logs-{_appSettings.Value.AppEnvironment}"));
            return indexResponse.IsValid;
        }


        public Task LogAction(ActivityLog activityLog)
        {
            throw new NotImplementedException();
        }

        public async Task ServiceLog(ServiceLog serviceLog, int retries = 2)
        {
            try
            {
                ServiceLogTime serviceLogTime = _serviceLogTime.FirstOrDefault(a => a.serviceTypeId == serviceLog.id);

                if (serviceLogTime == null || 
                    serviceLogTime?.lastLogTime < DateTime.Now.AddMinutes(-1) || 
                    serviceLog.status == enServiceStatus.fail.ToString())
                {
                    var currentLog = await _elasticClient.SearchAsync<ServiceLog>(c => c.Index(_appSettings.Value.Elasticsearch.service_log_index)
                   .Query(a => a.Term(c => c
                      .Field(p => p.serviceName.Suffix("keyword"))
                      .Value(serviceLog.serviceName)
                     )));

                    ServiceLog currentDoc = currentLog.Documents.FirstOrDefault();

                    if (currentDoc != null && currentDoc.status == enServiceStatus.fail.ToString())
                        serviceLog.status = enServiceStatus.fail.ToString();

                    serviceLog.unixtime = CommonHelper.GetCurrentUtcEpochTimeInSeconds();

                    await _elasticClient.IndexAsync(new IndexRequest<ServiceLog>(serviceLog, _appSettings.Value.Elasticsearch.service_log_index));

                    if (serviceLogTime == null)
                    {
                        _serviceLogTime.Add(new ServiceLogTime()
                        {
                            serviceTypeId = serviceLog.id,
                            lastLogTime = DateTime.Now
                        });
                    }
                    else {
                        serviceLogTime.lastLogTime = DateTime.Now;
                    }                    
                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "ServiceLog | Retry: {Retry} | Module: {Module}", retries, "Service Log " + serviceLog?.serviceName);
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    await ServiceLog(serviceLog,retries - 1);
                } 
                _logger.LogError(ex, "ServiceLog - " + serviceLog?.serviceName);
            }
        }
    }
}
