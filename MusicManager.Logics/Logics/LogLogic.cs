using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class LogLogic : ILogLogic
    {
        public IUnitOfWork _unitOfWork { get; }
        public ILogger<LogLogic> _Logger { get; }
        public IElasticLogic _elasticLogic { get; }

        public LogLogic(IUnitOfWork unitOfWork, 
            IOptions<AppSettings> appSettings, 
            ILogger<LogLogic> logger,
            IElasticLogic elasticLogic)
        {
            _unitOfWork = unitOfWork;
            _Logger = logger;
            _elasticLogic = elasticLogic;
        }

        public void LogErrors((string error, Guid id, string reason)[] errors)
        {
            List<log_track_index_error> list = new List<log_track_index_error>();
            foreach (var item in errors)
            {
                list.Add(new log_track_index_error()
                {
                    doc_id = item.id,
                    error = item.error,
                    reson = item.reason
                });
            }
            _unitOfWork.LogElasticTrackChange.LogErrors(list);

            _Logger.LogError($"Elastic index Error - {errors[0].reason}");
        }

        public async Task<SyncSummary> GetDailySyncSummaryCount(string orgId,DateTime summaryDate)
        {
            SyncSummary syncSummary = new SyncSummary();
            syncSummary.New_Tracks_Count = await _unitOfWork.TrackOrg.GetNewTracksCount(DateTime.Now.AddDays(-1), orgId);
            syncSummary.Updated_Tracks_Count = await _unitOfWork.MLMasterTrack.GetTrackUpdatedCountCount(DateTime.Now.AddDays(-1));
            syncSummary.PRS_Search_Count = await _elasticLogic.GetTrackCountByQueryAndDate("_exists_:prsFound", DateTime.Now.AddDays(-1));
            syncSummary.PRS_Found_Count = await _elasticLogic.GetTrackCountByQueryAndDate("prsFound:true", DateTime.Now.AddDays(-1));
            syncSummary.PRS_not_Found_Count = await _elasticLogic.GetTrackCountByQueryAndDate("prsFound:false", DateTime.Now.AddDays(-1));
            return syncSummary;
        }
    }
}
