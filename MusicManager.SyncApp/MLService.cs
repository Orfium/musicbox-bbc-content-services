using Microsoft.Extensions.Configuration;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Nest;
using Microsoft.Extensions.Options;
using System.IO;
using MusicManager.Application.Services;
using MusicManager.Logics.ServiceLogics;
using MusicManager.Core.Payload;
using System.Text.RegularExpressions;
using MusicManager.Logics.Logics;
using MusicManager.Logics.Helper;

namespace MusicManager.SyncApp
{
    public class MLService : IMLService
    {
        private readonly ILogger<MLService> _logger;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IAWSS3Repository _aWSS3Repository;
        private readonly IDHTrackSync _dHTrackSync;        
        private readonly IElasticLogic _elasticLogic;
        private readonly ICtagLogic _ctagLogic;
        private readonly IActionLoggerLogic _actionLoggerLogic;
        private readonly ILogLogic _logLogic;
        private readonly IPlayoutLogic _playoutLogic;
        private readonly ILibraryWorkspaceActionLogic _libraryWorkspaceActionLogic;
        private DateTime _takedownProcessDate= DateTime.Now.AddDays(-1);
        private DateTime _searchableByValidFromDate = DateTime.Now.AddDays(-1);
        private DateTime _preReleaseByValidFrom = DateTime.Now.AddDays(-1);
        private DateTime _prsSearchDate = DateTime.Now.AddDays(-1);
        private DateTime _chartTracksAndAlbumSyncDate = DateTime.Now.AddDays(-1);

        //private Guid AppUserId = new Guid("ba19b691-d01d-4b18-82cb-a41ec219f41e");
        private int UserId = 59;

        public MLService(ILogger<MLService> logger, IConfiguration config,
            IUnitOfWork unitOfWork,
            IOptions<AppSettings> appSettings,
            IAWSS3Repository AWSS3Repository,
            IDHTrackSync dHTrackSync,           
            IElasticLogic elasticLogic,
            ICtagLogic ctagLogic,
            IActionLoggerLogic actionLoggerLogic,
            ILogLogic logLogic,
            IPlayoutLogic playoutLogic,
            ILibraryWorkspaceActionLogic libraryWorkspaceActionLogic)
        {
            _logger = logger;
            _config = config;
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
            _aWSS3Repository = AWSS3Repository;
            _dHTrackSync = dHTrackSync;           
            _elasticLogic = elasticLogic;
            _ctagLogic = ctagLogic;
            _actionLoggerLogic = actionLoggerLogic;
            _logLogic = logLogic;
            _playoutLogic = playoutLogic;
            _libraryWorkspaceActionLogic = libraryWorkspaceActionLogic;
        }

        public async Task TuneCodeISRCImport(string path)
        {
            using (var reader = new StreamReader(path))
            {

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    var isExist = await _unitOfWork.TunecodeIsrc.IsExist(values[1], values[0]);
                    if (!isExist)
                    {
                        Console.WriteLine($"Adding {values[1]} and Tunecode {values[0]}");
                        await _unitOfWork.TunecodeIsrc.AddIsrcTunecode(values[1], values[0]);
                    }
                    else
                    {
                        Console.WriteLine($"ISRC {values[1]} and Tunecode {values[0]} is exist.");
                    }

                }
            }
        }

        public async Task SyncMasterCharts()
        {
            if (_chartTracksAndAlbumSyncDate.Date != DateTime.Now.Date) {               

                string trackChartTypeId = "b3c7f8d9-f5b7-4856-a933-25539e63ee37";
                string albumChartTypeId = "a8fc9a18-e137-49dd-bdaf-9eae00f41d88";

                //chart_sync_summary trackChartSyncSummary = await _unitOfWork.Chart.GetLastChartSync(Guid.Parse(trackChartTypeId), "t");
                //chart_sync_summary albumChartSyncSummary = await _unitOfWork.Chart.GetLastChartSync(Guid.Parse(albumChartTypeId), "a");

                //_logger.LogDebug("Master track - " + trackChartSyncSummary?.check_date);
                //_logger.LogDebug("Master album - " + albumChartSyncSummary?.check_date);

                chart_sync_summary trackChartSyncSummary = new chart_sync_summary()
                {
                    chart_type_id = Guid.Parse(trackChartTypeId),
                    type = "t",
                    check_date = DateTime.Now.AddMonths(-1)                    
                };

                chart_sync_summary albumChartSyncSummary = new chart_sync_summary()
                {
                    chart_type_id = Guid.Parse(trackChartTypeId),
                    type = "a",
                    check_date = DateTime.Now.AddMonths(-1)
                };

                MasterTrackChartResponse masterTrackChartResponse = await _unitOfWork.Chart.GetAllTrackMasterTracks(trackChartSyncSummary, trackChartTypeId);
                _logger.LogDebug("Master track count - " + masterTrackChartResponse?.results.Count());
                if (masterTrackChartResponse?.results.Count() > 0)
                {
                    int count = await _unitOfWork.Chart.InsertUpdateMasterTracksBulk(masterTrackChartResponse.results);
                    _logger.LogInformation("Imported track count - " + count);
                }

                MasterAlbumChartResponse masterAlbumChartResponse = await _unitOfWork.Chart.GetAllMasterAlbums(albumChartSyncSummary, albumChartTypeId);
                _logger.LogDebug("Master album count - " + masterAlbumChartResponse?.results.Count());
                if (masterAlbumChartResponse?.results.Count() > 0)
                {
                    int count = await _unitOfWork.Chart.InsertUpdateMasterAlbumBulk(masterAlbumChartResponse.results);
                    _logger.LogInformation("Imported album count - " + count);
                }
                _chartTracksAndAlbumSyncDate = DateTime.Now;
            }           
        }


        public async Task ClearCtags()
        {
            int size = 500;
            int indexedCount = 0;

            List<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchCtagedDocs(size);

            while (mLTrackDocuments.Count() > 0)
            {
                for (int j = 0; j < mLTrackDocuments.Count; j++)
                {
                    mLTrackDocuments[j].cTag2 = null;
                    mLTrackDocuments[j].cTag4 = null;
                    mLTrackDocuments[j].cTag5 = null;
                    mLTrackDocuments[j].cTag6 = null;
                    mLTrackDocuments[j].cTags = null;
                    mLTrackDocuments[j].prsSearchDateTime = null;
                    mLTrackDocuments[j].prsWorkPublishers = null;
                    mLTrackDocuments[j].prsWorkTitle = null;
                    mLTrackDocuments[j].prsWorkTunecode = null;
                    mLTrackDocuments[j].prsWorkWriters = null;
                }
                var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments);

                if (asyncIndexResponse.Length == 0)
                {
                    indexedCount += mLTrackDocuments.Count();
                    _logger.LogDebug("Track Indexed count - " + indexedCount);
                }

                mLTrackDocuments = await _elasticLogic.SearchCtagedDocs(size);
            }
        }

        public async Task IndexedCtags()
        {
            try
            {
                IEnumerable<c_tag> cTags = await _unitOfWork.CTags.GetIndexedCtags();
                int size = 10;
                int i = 0;
                int indexedCount = 0;

                _logger.LogDebug("Indexed Ctags count - " + cTags?.Count());

                c_tag_index_status cTagIndexStatus = await _unitOfWork.CTags.GetCtagIndexStatusByType("indexed");

                try
                {
                    if (cTags.Count() > 0 && cTagIndexStatus != null)
                    {
                        List<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchTracksForIndexCtags(size, cTagIndexStatus.update_idetifier);

                        while (mLTrackDocuments.Count() > 0)
                        {
                            if (i % 2 == 0)
                            {
                                cTagIndexStatus = await _unitOfWork.CTags.GetCtagIndexStatusByType("indexed");
                                cTags = await _unitOfWork.CTags.GetIndexedCtags();
                            }

                            for (int j = 0; j < mLTrackDocuments.Count; j++)
                            {
                                mLTrackDocuments[j].indexed_ctags = new List<int>();
                                mLTrackDocuments[j].indexed_ctag_idetifier = cTagIndexStatus.update_idetifier;
                                mLTrackDocuments[j].indexed_ctag_on = DateTime.Now;
                                

                                foreach (var cTag in cTags)
                                {
                                    CtagRuleCheck ctagRuleCheck = await _ctagLogic.CheckRules(mLTrackDocuments[j], cTag.id);
                                    if (ctagRuleCheck?.result == true)
                                    {
                                        mLTrackDocuments[j].takedownDate = DateTime.Now;
                                        mLTrackDocuments[j].indexed_ctags.Add(cTag.id);
                                        mLTrackDocuments[j].takedownType = enTakedownType.CTAG.ToString();
                                        mLTrackDocuments[j].archived = true;
                                    }
                                }
                            }

                            var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments);

                            if (asyncIndexResponse.Length == 0)
                            {
                                indexedCount += mLTrackDocuments.Count();
                                _logger.LogDebug("Track Indexed count - " + indexedCount);
                            }

                            if (cTags.Count() > 0 && cTagIndexStatus != null)
                                mLTrackDocuments = await _elasticLogic.SearchTracksForIndexCtags(size, cTagIndexStatus.update_idetifier);

                            i++;
                        }

                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)enServiceType.Index_Ctag_Service,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = enServiceType.Index_Ctag_Service.ToString(),
                            timestamp = DateTime.Now
                        });
                    }
                }
                catch (Exception ex)
                {
                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Index_Ctag_Service,
                        status = enServiceStatus.fail.ToString(),
                        serviceName = enServiceType.Index_Ctag_Service.ToString(),
                        timestamp = DateTime.Now
                    });
                    _logger.LogError(ex, "IndexedCtags");                  
                }
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Index_Ctag_Service,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.Index_Ctag_Service.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "IndexedCtags");
            }
        }

        public async Task TakedownByValidTo()
        {
            int size = 200;
            long completedCount = 0;
            _logger.LogDebug("TakedownByValidTo service - " + DateTime.Now);

            if (_takedownProcessDate.Date < DateTime.Now.Date && _appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour)
            {
                //--- Trigger SyncMasterCharts service
                _ = Task.Run(() => SyncMasterCharts()).ConfigureAwait(false);

                _logger.LogDebug("TakedownByValidTo service - Start - " + DateTime.Now);

                DateTime startTime = DateTime.Now;
                long toBeTakedownCount = await _elasticLogic.GetTakedownTracksCount();
                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchTakedownTracks(size);

                _logger.LogDebug("TakedownByValidTo Count - " + toBeTakedownCount);

                if (toBeTakedownCount > 0)
                {
                    while (mLTrackDocuments.Count() > 0)
                    {
                        mLTrackDocuments.ToList().ForEach(c => { c.archived = true; c.sourceDeleted = true; c.takedownDate = DateTime.Now; c.takedownType = enTakedownType.EXP.ToString(); });

                        var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments.ToList());

                        if (asyncIndexResponse.Length > 0)
                        {
                            completedCount += mLTrackDocuments.Count() - asyncIndexResponse.Length;
                            _logger.LogError("TakedownByValidTo > " + asyncIndexResponse.ToString());
                        }
                        else
                        {
                            completedCount += mLTrackDocuments.Count();
                        }                        

                        if (!(_appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                            && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour))
                            break;

                        await Task.Delay(TimeSpan.FromSeconds(5));
                      
                        mLTrackDocuments = await _elasticLogic.SearchTakedownTracks(size);

                        _logger.LogDebug("Completed (TakedownByValidTo) - " + completedCount + " / " + toBeTakedownCount);
                    }

                    var Summary = new { service_start_datetime = startTime, service_end_datetime = DateTime.Now, 
                        tracks_to_be_takendown = toBeTakedownCount, completed_count = completedCount
                    };

                    _logger.LogInformation(enServiceType.Takedown_Service.ToString() + " Start - {@startTime} / End - {@endTime} - {@summary}", startTime, Summary.service_end_datetime, Summary);

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Takedown_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Takedown_Service.ToString(),
                        timestamp = DateTime.Now,
                        summary = Summary
                    });
                }

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Takedown_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Takedown_Service.ToString(),
                    timestamp = DateTime.Now
                });

                _takedownProcessDate = DateTime.Now;                
            }

            await Task.Delay(TimeSpan.FromSeconds(60));

            await TakedownByValidTo();
        }

        public async Task SearchableByValidFrom()
        {
            int size = 200;
            long completedCount = 0;
            _logger.LogDebug("TakedownByValidFrom service  - " + DateTime.Now);

            if (_searchableByValidFromDate.Date < DateTime.Now.Date && _appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour)
            {
                DateTime startTime = DateTime.Now;
                long preReleaseCount = await _elasticLogic.GetNotPreReleaseTracksCount();
                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchNotPreReleaseTracks(size);

                if (preReleaseCount > 0)
                {
                    while (mLTrackDocuments.Count() > 0)
                    {
                        mLTrackDocuments.ToList().ForEach(c => { c.preRelease = false; c.searchableFrom = DateTime.Now; c.searchableType =  enPreReleaseType.EXP.ToString(); });

                        var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments.ToList());

                        if (asyncIndexResponse.Length > 0)
                        {
                            completedCount += mLTrackDocuments.Count() - asyncIndexResponse.Length;
                            _logger.LogError("PreReleaseByValidFrom > " + asyncIndexResponse.ToString());
                        }
                        else
                        {
                            completedCount += mLTrackDocuments.Count();
                        }

                        if (!(_appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                            && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour))
                            break;

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        mLTrackDocuments = await _elasticLogic.SearchNotPreReleaseTracks(size);

                        _logger.LogDebug("Completed (SearchableByValidFrom) - " + completedCount + " / " + preReleaseCount);
                    }

                    var Summary = new
                    {
                        service_start_datetime = startTime,
                        service_end_datetime = DateTime.Now,
                        searchable_count = preReleaseCount,
                        completed_count = completedCount
                    };

                    _logger.LogInformation(enServiceType.Set_Searchable_Service.ToString() + " Start - {@startTime} / End - {@endTime} - {@summary}", startTime, Summary.service_end_datetime, Summary);

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Set_Searchable_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Set_Searchable_Service.ToString(),
                        timestamp = DateTime.Now,
                        summary = Summary
                    });
                }
                _searchableByValidFromDate = DateTime.Now;

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Set_Searchable_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Set_Searchable_Service.ToString(),
                    timestamp = DateTime.Now
                });
            }

            await Task.Delay(TimeSpan.FromSeconds(60));

            await SearchableByValidFrom();
        }

        public async Task PreReleaseByValidFrom()
        {
            int size = 200;
            long completedCount = 0;
            _logger.LogDebug("PreReleaseByValidFrom service  - " + DateTime.Now);

            if (_preReleaseByValidFrom.Date < DateTime.Now.Date && _appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour)
            {
                DateTime startTime = DateTime.Now;
                long preReleaseCount = await _elasticLogic.GetPreReleaseTracksCount();
                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchPreReleaseTracks(size);

                if (preReleaseCount > 0)
                {
                    while (mLTrackDocuments.Count() > 0)
                    {
                        mLTrackDocuments.ToList().ForEach(c => { c.preRelease = true; c.searchableFrom = null; c.searchableType = enPreReleaseType.DH.ToString(); });

                        var asyncIndexResponse = await _elasticLogic.BulkIndexTrackDocument(mLTrackDocuments.ToList());

                        if (asyncIndexResponse.Length > 0)
                        {
                            completedCount += mLTrackDocuments.Count() - asyncIndexResponse.Length;
                            _logger.LogError("PreReleaseByValidFrom > " + asyncIndexResponse.ToString());
                        }
                        else
                        {
                            completedCount += mLTrackDocuments.Count();
                        }

                        if (!(_appSettings.Value.ServiceScheduleTimes.TakeDownStartHour <= DateTime.Now.Hour
                            && _appSettings.Value.ServiceScheduleTimes.TakeDownEndhour >= DateTime.Now.Hour))
                            break;

                        await Task.Delay(TimeSpan.FromSeconds(5));

                        mLTrackDocuments = await _elasticLogic.SearchPreReleaseTracks(size);

                        _logger.LogDebug("Completed (PreReleaseByValidFrom)- " + completedCount + " / " + preReleaseCount);
                    }

                    var Summary = new
                    {
                        service_start_datetime = startTime,
                        service_end_datetime = DateTime.Now,
                        pre_release_count = preReleaseCount,
                        completed_count = completedCount
                    };

                    _logger.LogInformation(enServiceType.Set_Pre_Release_Service.ToString() + " Start - {@startTime} / End - {@endTime} - {@summary}", startTime, Summary.service_end_datetime, Summary);

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Set_Pre_Release_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Set_Pre_Release_Service.ToString(),
                        timestamp = DateTime.Now,
                        summary = Summary
                    });
                }
                _preReleaseByValidFrom = DateTime.Now;

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Set_Pre_Release_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Set_Pre_Release_Service.ToString(),
                    timestamp = DateTime.Now
                });
            }

            await Task.Delay(TimeSpan.FromSeconds(60));

            await PreReleaseByValidFrom();
        }

        public async Task PRSIndex(bool charted)
        {
            int size = 300;
            int indexedCount = 0;
            int totalTrackCount = 0;
            int prsSessionError = 0;
            int prsFound = 0;
            int prsNotFound = 0;
            PRSUpdateReturn pRSUpdateReturn = null;
            List<c_tag> c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;
            List<MLTrackDocument> indexDocumnets = new List<MLTrackDocument>();

            _logger.LogDebug("PRSIndex service 1 - " + DateTime.Now);

            try
            {

                _logger.LogDebug("PRSIndex service - Start - " + DateTime.Now);
                _logger.LogInformation("PRSIndex service - Start - " + DateTime.Now);

                List<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchTracksForPRSIndex(size);

                if (mLTrackDocuments.Count() == 0)
                {
                    _logger.LogInformation("PRSIndex service - ascending - completed" + DateTime.Now);
                }

                while (mLTrackDocuments.Count() > 0)
                {
                    indexedCount = 0;
                    prsFound = 0;
                    prsNotFound = 0;                  

                    totalTrackCount += mLTrackDocuments.Count();

                    for (int j = 0; j < mLTrackDocuments.Count; j++)
                    {
                        pRSUpdateReturn = await _ctagLogic.UpdatePRSforTrack(mLTrackDocuments[j].id, mLTrackDocuments[j], c_Tags, charted, false);

                        //--- If the PRS session id is not returned add 5min delay
                        if (pRSUpdateReturn?.prsSessionNotFound == true)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(5));
                        }

                        //--- If the PRS search is failed add 1 min delay
                        if (pRSUpdateReturn?.prsSearchError == true)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1));
                        }
                        

                        if (pRSUpdateReturn?.prsSearchError != true 
                            && pRSUpdateReturn?.mLTrackDocument != null
                            && pRSUpdateReturn?.prsSessionNotFound != true)
                            indexDocumnets.Add(pRSUpdateReturn?.mLTrackDocument);

                        if (pRSUpdateReturn?.prsFound == true)
                        {
                            _logger.LogDebug("PRS found - " + mLTrackDocuments[j].id);
                            prsFound++;
                        }
                        else if (pRSUpdateReturn.prsSessionNotFound)
                        {
                            prsSessionError++;
                        }
                        else
                        {                           
                            prsNotFound++;
                        }
                        indexedCount++;
                    }

                    _logger.LogInformation("PRS Index summary - {indexedCount} (Found: {prsFound} / Not found: {prsNotFound} / Session Error: {prsSessionError}) | Total {@totalTrackCount}, Module:{Module}", indexedCount, prsFound, prsNotFound, prsSessionError, totalTrackCount,"PRS Service");


                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.PRS_Index_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.PRS_Index_Service.ToString(),
                        timestamp = DateTime.Now
                    });

                    if (indexDocumnets.Count() > 0)
                    {
                        await _elasticLogic.BulkIndexTrackDocument(indexDocumnets);
                    }

                    indexDocumnets = new List<MLTrackDocument>();
                    mLTrackDocuments = await _elasticLogic.SearchTracksForPRSIndex(size);

                    _logger.LogDebug("PRSIndex service - End - " + DateTime.Now);
                }
                _prsSearchDate = DateTime.Now;


                await Task.Delay(TimeSpan.FromSeconds(60));
                await PRSIndex(false);
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.PRS_Index_Service,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.PRS_Index_Service.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "PRSIndex");

                await Task.Delay(TimeSpan.FromSeconds(60));
                await PRSIndex(false);
            }
        }

        public async Task PRSIndex2(bool charted)
        {
            int size = 300;
            int indexedCount = 0;
            int trackCount = 0;
            int prsFound = 0;
            int prsSessionError = 0;
            int prsNotFound = 0;
            PRSUpdateReturn pRSUpdateReturn = null;
            List<c_tag> c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;
            List<MLTrackDocument> indexDocumnets = new List<MLTrackDocument>();

            _logger.LogDebug("PRSIndex service 2 - " + DateTime.Now);

            try
            {
                //if (_prsSearchDate.Date < DateTime.Now.Date && _appSettings.Value.ServiceScheduleTimes.PRSSearchStartHour <= DateTime.Now.Hour
                //&& _appSettings.Value.ServiceScheduleTimes.PRSSearchEndtHour >= DateTime.Now.Hour)
                //{

                _logger.LogDebug("PRSIndex service - Start - " + DateTime.Now);
                _logger.LogInformation("PRSIndex service - Start - " + DateTime.Now);

                List<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchTracksForPRSIndex2(size);

                if (mLTrackDocuments.Count() == 0)
                {
                    _logger.LogInformation("PRSIndex service - decending - completed" + DateTime.Now);
                }

                while (mLTrackDocuments.Count() > 0)
                {
                    _logger.LogDebug("PRS Indexed 2 - " + mLTrackDocuments.Count());

                    trackCount += mLTrackDocuments.Count();

                    for (int j = 0; j < mLTrackDocuments.Count; j++)
                    {
                        pRSUpdateReturn = await _ctagLogic.UpdatePRSforTrack(mLTrackDocuments[j].id, mLTrackDocuments[j], c_Tags, charted, false);

                        if (pRSUpdateReturn.prsSearchError != true &&
                            pRSUpdateReturn.mLTrackDocument != null &&
                            pRSUpdateReturn.prsSessionNotFound != true)
                            indexDocumnets.Add(pRSUpdateReturn.mLTrackDocument);

                        if (pRSUpdateReturn.prsFound == true)
                        {
                            _logger.LogDebug("PRS found - " + mLTrackDocuments[j].id);
                            prsFound++;
                        }
                        else if (pRSUpdateReturn.prsSessionNotFound) {
                            prsSessionError++;
                        }
                        else
                        {
                            _logger.LogDebug("PRS Not found - " + mLTrackDocuments[j].id);
                            prsNotFound++;
                        }
                        indexedCount++;                       
                    }                  

                    _logger.LogInformation($"PRS Index summary - { indexedCount} (Found: {prsFound} - Not found: {prsNotFound} - Session Error: {prsSessionError})/{trackCount}");

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.PRS_Index_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.PRS_Index_Service.ToString(),
                        timestamp = DateTime.Now
                    });

                    if (indexDocumnets.Count() > 0)
                    {
                        await _elasticLogic.BulkIndexTrackDocument(indexDocumnets);
                    }

                    //if (!(_appSettings.Value.ServiceScheduleTimes.PRSSearchStartHour <= DateTime.Now.Hour
                    //    && _appSettings.Value.ServiceScheduleTimes.PRSSearchEndtHour >= DateTime.Now.Hour))
                    //    break;

                    indexDocumnets = new List<MLTrackDocument>();
                    mLTrackDocuments = await _elasticLogic.SearchTracksForPRSIndex2(size);
                   
                    _logger.LogDebug("PRSIndex service - End - " + DateTime.Now);                   
                }

                _prsSearchDate = DateTime.Now;
                //}

                await Task.Delay(TimeSpan.FromSeconds(60));
                await PRSIndex(false);
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.PRS_Index_Service,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.PRS_Index_Service.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "PRSIndex");

                await Task.Delay(TimeSpan.FromSeconds(60));
                await PRSIndex(false);
            }
        }

        public async Task ChartPRSIndex(bool charted,string path)
        {    


            int size = 100;
            int indexedCount = 0;
            int notFoundCount = 0;
            int trackCount = 0;

            try
            {
                List<c_tag> c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;
                List<Guid> trackIds = await ReadCSV(path);

                IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.SearchTracksForPRSIndex(trackIds.Take(trackIds.Count() >= size ? size : trackIds.Count()).ToArray());
                while (trackIds.Count() > 0)
                {
                    _logger.LogDebug("Track Count - " + trackIds.Count());

                    _logger.LogDebug("PRS Indexed - " + mLTrackDocuments.Count());

                    trackCount += mLTrackDocuments.Count();

                    foreach (var item in mLTrackDocuments)
                    {
                        PRSUpdateReturn pRSUpdateReturn = await _ctagLogic.UpdatePRSforTrack(item.id, item, c_Tags, charted);
                        if (pRSUpdateReturn.prsFound == true)
                        {
                            indexedCount++;
                        }
                        else
                        {
                            notFoundCount++;
                        }
                        _logger.LogDebug("PRS Indexed - " + indexedCount + " / " + trackCount);
                    }

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.PRS_Index_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.PRS_Index_Service.ToString(),
                        timestamp = DateTime.Now
                    });

                    trackIds.RemoveRange(0, trackIds.Count() >= size ? size : trackIds.Count());

                    mLTrackDocuments = await _elasticLogic.SearchTracksForPRSIndex(trackIds.Take(10).ToArray());

                    if (trackIds.Count() == 0)
                    {
                        await Task.Delay(30000);
                        trackIds = await ReadCSV(path);
                    }
                }
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.PRS_Index_Service,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.PRS_Index_Service.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "ChartPRSIndex");
            }
        }

        public async Task UpdateAlbumChartIndicator(string path)
        {
            int indexedCount = 0;
            int trackCount = 0;
            int notFoundCount = 0;
            int readyToIndex = 0;
            int bulkCount = 10;

            try
            {
                List<AlbumChartInfo> albumCharts = await ReadChartedCSV(path);
                List<album_org> albumOrgs = new List<album_org>();               

                foreach (var item in albumCharts)
                {
                    trackCount++;
                    MLAlbumDocument mLAlbumDocument = await _elasticLogic.GetElasticAlbumByProdId(Guid.Parse(item.dh_album_id));
                    if (mLAlbumDocument != null)
                    {
                        if (mLAlbumDocument.charted == true)
                        {
                            _logger.LogDebug("Already charted");
                        }
                        else
                        {
                            readyToIndex++;
                            albumOrgs.Add(new album_org()
                            {
                                id = (Guid)mLAlbumDocument.id,
                                chart_info = JsonConvert.SerializeObject(item, new JsonSerializerSettings()),
                                original_album_id = (Guid)mLAlbumDocument.prodId,
                                org_id = "N2eu7wCxhyhmoj0FXgSF"
                            });

                            if (readyToIndex % bulkCount == 0)
                            {
                                indexedCount += await _unitOfWork.Album.UpdateChartInfoById(albumOrgs);
                                albumOrgs = new List<album_org>();
                            }
                        }
                    }
                    else
                    {
                        notFoundCount++;
                    }
                    _logger.LogDebug("Total - " + albumCharts.Count() + " / Checked - " + trackCount + " / Completed - " + indexedCount + " / Not found - " + notFoundCount);
                }

                if (albumOrgs.Count() > 0)
                    indexedCount += await _unitOfWork.Album.UpdateChartInfoById(albumOrgs);

                _logger.LogDebug("Total - " + albumCharts.Count() + " / Checked - " + trackCount + " / Completed - " + indexedCount + " / Not found - " + notFoundCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"UpdateChartIndicator");
            }
        }

        public async Task UpdateTrackChartIndicator(string path)
        {
            int indexedCount = 0;
            int trackCount = 0;
            int notFoundCount = 0;
            int bulkCount = 100;
            int readyToIndex = 0;

            try
            {
                List<TrackChartInfo> albumCharts = await ReadChartedTrackCSV(path);

                _logger.LogDebug("Total - " + albumCharts.Count());

                List<track_org> trackOrgs = new List<track_org>();

                foreach (var item in albumCharts)
                {
                    trackCount++;
                    MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocByDhTrackId(Guid.Parse(item.dh_track_id));
                    if (mLTrackDocument != null)
                    {
                        if (mLTrackDocument.charted == true && !string.IsNullOrEmpty(mLTrackDocument.chartType))
                        {
                            _logger.LogDebug("Already charted - " + mLTrackDocument.id);
                        }
                        else
                        {
                            readyToIndex++;
                            _logger.LogDebug("Track found - " + mLTrackDocument.id);

                            //track_org trackOrg = await _unitOfWork.TrackOrg.GetById((Guid)mLTrackDocument.id);

                            trackOrgs.Add(new track_org()
                            {
                                id = (Guid)mLTrackDocument.id,
                                chart_info = JsonConvert.SerializeObject(item, new JsonSerializerSettings()),
                                original_track_id = (Guid)mLTrackDocument.dhTrackId,
                                org_id = mLTrackDocument.org_id
                            });

                            if (readyToIndex % bulkCount == 0)
                            {
                                indexedCount += await _unitOfWork.TrackOrg.UpdateChartInfoBulk(trackOrgs);
                                trackOrgs = new List<track_org>();
                            }

                            //if (trackOrg != null)
                            //{
                            //await _unitOfWork.TrackOrg.UpdateChartInfo(item, (Guid)mLTrackDocument.id);
                            //indexedCount++;
                            ////}
                        }

                    }
                    else
                    {
                        _logger.LogDebug("Not found - " + item.dh_track_id);
                        notFoundCount++;
                    }

                    _logger.LogDebug("Total - " + albumCharts.Count() + " / Checked - " + trackCount + " / Completed - " + indexedCount + " / Not found - " + notFoundCount);
                }

                indexedCount += await _unitOfWork.TrackOrg.UpdateChartInfoBulk(trackOrgs);
                _logger.LogDebug("Total - " + albumCharts.Count() + " / Checked - " + trackCount + " / Completed - " + indexedCount + " / Not found - " + notFoundCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"UpdateChartIndicator");
            }
        }      

        private async Task<List<Guid>> ReadCSV(string path)
        {
            List<Guid> ids = new List<Guid>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split('\t');                    
                    if (values.Count()>0)
                    {
                        ids.Add(Guid.Parse(values[0].Replace("\"","")));
                    }                    
                }
            }
            return ids;
        }

        private async Task<List<AlbumChartInfo>> ReadChartedCSV(string path)
        {
            List<AlbumChartInfo> list = new List<AlbumChartInfo>();

            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var values = line.Split('\t');
                    if (values.Count() > 0)
                    {
                        list.Add(new AlbumChartInfo() { 
                            master_album_id = values[0].TrimStart('"').TrimEnd('"'),
                            dh_album_id = values[1].TrimStart('"').TrimEnd('"'),
                            dh_workspace_id = values[2].TrimStart('"').TrimEnd('"'),
                            first_date_released = values[3].TrimStart('"').TrimEnd('"'),
                            first_pos = values[4].TrimStart('"').TrimEnd('"'),
                            highest_date_released = values[5].TrimStart('"').TrimEnd('"'),
                            highest_pos = values[6].TrimStart('"').TrimEnd('"'),
                            chart_type_id = values[7].TrimStart('"').TrimEnd('"'),
                            chart_type_name = values[8].TrimStart('"').TrimEnd('"'),
                        });
                    }
                }
            }
            return list;
        }

        private async Task<List<TrackChartInfo>> ReadChartedTrackCSV(string path)
        {
            List<TrackChartInfo> list = new List<TrackChartInfo>();

            try
            {
                using (var reader = new StreamReader(path))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        var values = line.Split('\t');
                        if (values.Count() > 0)
                        {
                            list.Add(new TrackChartInfo()
                            {
                                master_track_id = values[0].TrimStart('"').TrimEnd('"'),
                                dh_track_id = values[1].TrimStart('"').TrimEnd('"'),
                                dh_workspace_id = values[2].TrimStart('"').TrimEnd('"'),
                                first_date_released = values[3].TrimStart('"').TrimEnd('"'),
                                first_pos = values[4].TrimStart('"').TrimEnd('"'),
                                highest_date_released = values[5].TrimStart('"').TrimEnd('"'),
                                highest_pos = values[6].TrimStart('"').TrimEnd('"'),
                                chart_type_id = values[7].TrimStart('"').TrimEnd('"'),
                                chart_type_name = values[8].TrimStart('"').TrimEnd('"'),
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("CSV Read error - " + ex);
            }            
            return list;
        }

        public async Task SyncLibrary()
        {
            try
            {
                _logger.LogDebug("---------------------------------------------------------------");
                _logger.LogDebug("Retrieving Library list from Datahub - " + DateTime.Now);
               
                List<MetadataLibrary> MetadataLibraries = await _unitOfWork.MetadataAPI.GetAllLibraries();

                if (MetadataLibraries != null) {
                    _logger.LogDebug("Source Library count : " + MetadataLibraries.Count);

                    int lineNo = 1;

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Workspace_Library_Sync_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Workspace_Library_Sync_Service.ToString(),
                        timestamp = DateTime.Now
                    });

                    List<staging_library> stagingLibrary = new List<staging_library>();

                    foreach (MetadataLibrary item in MetadataLibraries)
                    {
                        stagingLibrary.Add(new staging_library()
                        {
                            library_name = item.name,
                            library_id = new Guid(item.id),
                            workspace_id = new Guid(item.workspaceid),
                            track_count = item.trackCount,
                            deleted = item.deleted,
                            date_created = CommonHelper.GetCurrentUtcEpochTime()
                        });

                        if (lineNo % 1000 == 0)
                        {
                            await _unitOfWork.LibraryStaging.BulkInsert(stagingLibrary);
                            stagingLibrary = new List<staging_library>();
                        }
                        lineNo++;
                    }

                    await _unitOfWork.LibraryStaging.BulkInsert(stagingLibrary);

                    _unitOfWork.Library.SyncLibraries(UserId);

                    _logger.LogDebug("Library sync completed - " + DateTime.Now);
                    _logger.LogDebug("---------------------------------------------------------------");
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncLibrary");
                throw;
            }            
        }

        public async Task SyncWS(bool init)
        {
            try
            {
                _logger.LogDebug("---------------------------------------------------------------");
                _logger.LogDebug("Retrieving Workspace list from Datahub - " + DateTime.Now);                
                List<MetadataWorkspace> MetadataWorkspaces = await _unitOfWork.MetadataAPI.GetAllWorkspaces();

                if (MetadataWorkspaces != null) {
                    _logger.LogDebug("Source Workspace count : " + MetadataWorkspaces.Count);

                    int lineNo = 1;

                    List<staging_workspace> stagingWorkspace = new List<staging_workspace>();

                    foreach (MetadataWorkspace item in MetadataWorkspaces)
                    {
                        stagingWorkspace.Add(new staging_workspace()
                        {
                            workspace_name = item.name,
                            workspace_id = new Guid(item.id),
                            track_count = item.trackCount,
                            deleted = item.deleted
                        });

                        if (lineNo % 1000 == 0)
                        {
                            await _unitOfWork.WorkspaceStaging.BulkInsert(stagingWorkspace);
                            stagingWorkspace = new List<staging_workspace>();
                        }

                        lineNo++;
                    }

                    await _unitOfWork.WorkspaceStaging.BulkInsert(stagingWorkspace);

                    _unitOfWork.Workspace.SyncWorkspaces(UserId);

                    _logger.LogDebug("Workspace sync completed - " + DateTime.Now);
                    _logger.LogDebug("---------------------------------------------------------------");

                    await _actionLoggerLogic.ServiceLog(new ServiceLog()
                    {
                        id = (int)enServiceType.Workspace_Library_Sync_Service,
                        status = enServiceStatus.pass.ToString(),
                        serviceName = enServiceType.Workspace_Library_Sync_Service.ToString(),
                        timestamp = DateTime.Now
                    });
                }               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncWS");
                throw;
            }            

            await SyncLibrary();

            await Task.Delay(TimeSpan.FromMinutes(30));

            await SyncNewLibrariesAfterSetLive();

            await Task.Delay(TimeSpan.FromMinutes(1));

            await SyncWS(false);
        }

        private async Task SyncNewLibrariesAfterSetLive()
        {
            IEnumerable<library> workspaces = await _unitOfWork.Library.GetNewDistinctWorkspacesAfterLive();

            foreach (library item in workspaces)
            {
                IEnumerable<workspace_org> workspaceOrgs = await _unitOfWork.Workspace.GetWorkspaceOrgsByWorkspaceId(item.workspace_id);

                foreach (workspace_org workspaceOrg in workspaceOrgs)
                {
                    _logger.LogInformation("SyncNewLibrariesAfterSetLive workspace id:{workspaceId} | Module:{module}", item.workspace_id, "Workspace Lib Sync");

                    IEnumerable<library> libraries = await _unitOfWork.Library.GetLibraryListByWorkspaceId(item.workspace_id);

                    foreach (var lib in libraries)
                    {
                        await _libraryWorkspaceActionLogic.CheckAndInsertLibraryOrg(lib.library_id, lib.workspace_id, workspaceOrg.org_id, workspaceOrg.created_by, (enMLStatus)workspaceOrg.ml_status);
                    }
                }

                //--- Resync workspace 
                SyncActionPayload syncActionPayload = new SyncActionPayload() { 
                    ids = new List<string>()
                };
                syncActionPayload.userId = "0";
                syncActionPayload.ids.Add(item.workspace_id.ToString());
                await _libraryWorkspaceActionLogic.Resync(syncActionPayload);
            }
        }

        public async Task DownloadDHTracks()
        {
            try
            {               

                IEnumerable<workspace> workspaces = await _unitOfWork.Workspace.GetWorkspacesForSyncAsync();

                if (workspaces?.Count() > 0)
                {
                    foreach (workspace ws in workspaces)
                    {
                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)enServiceType.Sync_External_Workspace,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = enServiceType.Sync_External_Workspace.ToString(),
                            timestamp = DateTime.Now,
                            refId = ws.workspace_id.ToString()
                        });

                        log_sync_time logSyncTime = new log_sync_time();
                       
                        if (ws.workspace_id != Guid.Parse(_appSettings.Value.MasterWSId) && await _unitOfWork.Workspace.CheckPause(ws.workspace_id) == false)
                        {
                            logSyncTime = new log_sync_time()
                            {
                                service_id = _appSettings.Value.SyncServiceId,
                                track_download_start_time = DateTime.Now,
                                workspace_id = ws.workspace_id
                            };

                            logSyncTime.download_tracks_count = await _dHTrackSync.DownloadDHTracks(ws, enServiceType.Sync_External_Workspace);
                            logSyncTime.track_download_end_time = DateTime.Now;
                            logSyncTime.track_download_time = logSyncTime.track_download_end_time - logSyncTime.track_download_start_time;


                            logSyncTime.album_download_start_time = DateTime.Now;
                            logSyncTime.download_albums_count = await _dHTrackSync.DownloadDHAlbums(ws, enServiceType.Sync_External_Workspace);
                            logSyncTime.album_download_end_time = DateTime.Now;
                            logSyncTime.album_download_time = logSyncTime.album_download_end_time - logSyncTime.album_download_start_time;


                            IEnumerable<workspace_org> workspace_Orgs = await _unitOfWork.Workspace.GetWorkspaceOrgsByWorkspaceId(ws.workspace_id);
                            foreach (var wsOrg in workspace_Orgs)
                            {
                                logSyncTime.org_id = wsOrg.org_id;

                                var isPaused = await _unitOfWork.Workspace.CheckPause(wsOrg.workspace_id);
                                if (!isPaused)
                                {
                                    logSyncTime.sync_start_time = DateTime.Now;

                                    await _dHTrackSync.SyncTracks(wsOrg, enServiceType.Sync_External_Workspace);

                                    logSyncTime.sync_end_time = DateTime.Now;

                                    logSyncTime.sync_time = logSyncTime.sync_end_time - logSyncTime.sync_start_time;

                                    logSyncTime.track_index_start_time = DateTime.Now;
                                    logSyncTime.index_tracks_count = await _dHTrackSync.ElasticIndex(wsOrg, enServiceType.Sync_External_Workspace);
                                    logSyncTime.track_index_end_time = DateTime.Now;
                                    logSyncTime.track_index_time = logSyncTime.track_index_end_time - logSyncTime.track_index_start_time;

                                    if (logSyncTime.index_tracks_count > 0)
                                        await Task.Delay(10000);

                                    logSyncTime.album_index_start_time = DateTime.Now;
                                    logSyncTime.index_albums_count = await _dHTrackSync.AlbumElasticIndex(wsOrg, enServiceType.Sync_External_Workspace);
                                    logSyncTime.album_index_end_time = DateTime.Now;
                                    logSyncTime.album_index_time = logSyncTime.album_index_end_time - logSyncTime.album_index_start_time;

                                }
                            }
                        }

                        if (logSyncTime.download_tracks_count > 0 ||
                           logSyncTime.index_albums_count > 0 ||
                           logSyncTime.index_tracks_count > 0
                           )
                        {
                            logSyncTime.total_time = logSyncTime.track_download_time + logSyncTime.album_download_time + logSyncTime.sync_time + logSyncTime.track_index_time + logSyncTime.album_index_time;
                            logSyncTime.id = await _unitOfWork.logSyncTime.Save(logSyncTime);
                            await _unitOfWork.logSyncTime.UpdateLogSyncTime(logSyncTime);

                            var Summary = new { download_tracks_count = logSyncTime.download_tracks_count, index_tracks_count = logSyncTime.index_tracks_count };
                            _logger.LogInformation(enServiceType.Sync_External_Workspace.ToString() + " ({@wsId}) - in {@time} time - {@Summary}", ws.workspace_id, logSyncTime.total_time, Summary);
                        }
                    }
                    _logger.LogDebug("Track Sync Completed Success");
                }
                else {
                    _logger.LogDebug("No workspace found");
                }                         

                await Task.Delay(TimeSpan.FromMinutes(5));              

                await DownloadDHTracks();                
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Sync_External_Workspace,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.Sync_External_Workspace.ToString(),
                    timestamp = DateTime.Now                   
                });

                _logger.LogError(ex, "Sync External Workspace");

                await Task.Delay(TimeSpan.FromMinutes(5));
                await DownloadDHTracks();
            }
        }

        public async Task DownloadMasterDHTracks()
        {

            try
            {
                IEnumerable<workspace> workspaces = await _unitOfWork.Workspace.GetMasterWorkspaceForSyncAsync(Guid.Parse(_appSettings.Value.MasterWSId));
                if (workspaces?.Count()>0)
                {
                    foreach (workspace workspace in workspaces)
                    {
                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)enServiceType.Sync_Master_Workspace,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = enServiceType.Sync_Master_Workspace.ToString(),
                            timestamp = DateTime.Now,
                            refId = workspace.workspace_id.ToString()
                        });

                        log_sync_time logSyncTime = new log_sync_time()
                        {
                            service_id = 0,
                            track_download_start_time = DateTime.Now,
                            workspace_id = workspace.workspace_id
                        };                      

                        logSyncTime.download_tracks_count = await _dHTrackSync.DownloadDHTracks(workspace, enServiceType.Sync_Master_Workspace);
                        logSyncTime.track_download_end_time = DateTime.Now;
                        logSyncTime.track_download_time = logSyncTime.track_download_end_time - logSyncTime.track_download_start_time;

                        logSyncTime.album_download_start_time = DateTime.Now;
                        logSyncTime.download_albums_count = await _dHTrackSync.DownloadDHAlbums(workspace, enServiceType.Sync_Master_Workspace);
                        logSyncTime.album_download_end_time = DateTime.Now;
                        logSyncTime.album_download_time = logSyncTime.album_download_end_time - logSyncTime.album_download_start_time;

                        IEnumerable<workspace_org> workspace_Orgs = await _unitOfWork.Workspace.GetWorkspaceOrgsByWorkspaceId(workspace.workspace_id);

                        foreach (var item in workspace_Orgs)
                        {
                            var isPaused = await _unitOfWork.Workspace.CheckPause(item.workspace_id);
                            if (!isPaused)
                            {
                                logSyncTime.sync_start_time = DateTime.Now;
                                await _dHTrackSync.SyncTracks(item, enServiceType.Sync_Master_Workspace);
                                logSyncTime.sync_end_time = DateTime.Now;
                                logSyncTime.sync_time = logSyncTime.sync_end_time - logSyncTime.sync_start_time;                              

                                logSyncTime.track_index_start_time = DateTime.Now;
                                logSyncTime.index_tracks_count = await _dHTrackSync.ElasticIndex(item, enServiceType.Sync_Master_Workspace);
                                logSyncTime.track_index_end_time = DateTime.Now;
                                logSyncTime.track_index_time = logSyncTime.track_index_end_time - logSyncTime.track_index_start_time;

                                if (logSyncTime.index_tracks_count > 0)
                                    await Task.Delay(2000);

                                logSyncTime.album_index_start_time = DateTime.Now;
                                logSyncTime.index_albums_count = await _dHTrackSync.AlbumElasticIndex(item, enServiceType.Sync_Master_Workspace);
                                logSyncTime.album_index_end_time = DateTime.Now;
                                logSyncTime.album_index_time = logSyncTime.album_index_end_time - logSyncTime.album_index_start_time;
                            }
                        }

                        if (logSyncTime.download_tracks_count > 0 ||
                           logSyncTime.download_albums_count > 0 ||
                           logSyncTime.index_albums_count > 0 ||
                           logSyncTime.index_tracks_count > 0
                           )
                        {
                            logSyncTime.total_time = logSyncTime.track_download_time + logSyncTime.album_download_time + logSyncTime.sync_time + logSyncTime.track_index_time + logSyncTime.album_index_time;
                            logSyncTime.id = await _unitOfWork.logSyncTime.Save(logSyncTime);
                            await _unitOfWork.logSyncTime.UpdateLogSyncTime(logSyncTime);

                            var Summary = new { download_tracks_count = logSyncTime.download_tracks_count, index_tracks_count = logSyncTime.index_tracks_count };
                            _logger.LogInformation(enServiceType.Sync_Master_Workspace.ToString() + " ({@wsId}) - in {@time} time - {@Summary}", workspace.workspace_id, logSyncTime.total_time, Summary);
                        }
                    }
                }
                
                await Task.Delay(TimeSpan.FromSeconds(10));
                await DownloadMasterDHTracks();
            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Sync_Master_Workspace,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.Sync_Master_Workspace.ToString(),
                    timestamp = DateTime.Now
                });

                _logger.LogError(ex, "DownloadMasterDHTracks");

                await Task.Delay(TimeSpan.FromSeconds(10));
                await DownloadMasterDHTracks();
            }
        }


        public async Task CreateTrackIndex()
        {
            await _elasticLogic.CreateTrackIndex();
        }

        public async Task CreateAlbumIndex()
        {
            await _elasticLogic.CreateAlbumIndex();
        }



        public static byte[] ReadFully(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        public async Task ProcessUploadedTracks()
        {
            try
            {
                List<upload_track> upload_Tracks = await _unitOfWork.UploadTrack.GetTracksForAssetUpload();

                if (upload_Tracks?.Count() > 0) {
                    List<MA_BulkUploadPayload> bulkUploadPayloads = new List<MA_BulkUploadPayload>();

                    _logger.LogDebug("Found - " + upload_Tracks.Count() + " tracks");

                    org_user org_User = null;

                    List<c_tag> c_Tags = await _unitOfWork.CTags.GetAllActiveCtags() as List<c_tag>;


                    int x = 0;

                    for (int i = 0; i < upload_Tracks.Count(); i++)
                    {
                        await _actionLoggerLogic.ServiceLog(new ServiceLog()
                        {
                            id = (int)enServiceType.Track_Upload_Service,
                            status = enServiceStatus.pass.ToString(),
                            serviceName = enServiceType.Track_Upload_Service.ToString(),
                            timestamp = DateTime.Now
                        });

                        org_User = await _unitOfWork.User.GetUserById((int)upload_Tracks[i].created_by);

                        if (org_User == null)
                            org_User = new org_user() { user_id = (int)upload_Tracks[i].created_by };

                        //string dhTrackId = null;
                        //string dhAlbumId = null;

                        #region -----------  Create Album ------------------------------------------------------------------------------------

                        //upload_album upload_Album = await _unitOfWork.UploadAlbum.FirstOrDefualt(a => a.id == upload_Tracks[i].ml_album_id);

                        //if (upload_Album != null && upload_Album?.dh_album_id == null)
                        //{
                        //    DHAlbum dHAlbum = await _unitOfWork.MusicAPI.PrepareAndCreateAlbum(_appSettings.Value.MasterWSId, upload_Album, org_User);

                        //    if (dHAlbum!=null)
                        //    {
                        //        upload_Tracks[i].dh_album_id = dHAlbum.id;
                        //        upload_Album.dh_album_id = dHAlbum.id;
                        //        upload_Album.modified = false;
                        //        await _unitOfWork.UploadAlbum.UpdateAlbumByDHAlbumId(upload_Album);
                        //    }
                        //}

                        #endregion

                        #region -----------  Upload Artwork ------------------------------------------------------------------------------------
                        //try
                        //{
                        //    if (upload_Tracks[i].dh_album_id != null && upload_Tracks[i].artwork_uploaded == true && upload_Album.artwork_uploaded == null)
                        //    {
                        //        string _artWorkKey = $"{_appSettings.Value.AWSS3.FolderName}/ARTWORK/{upload_Album.session_id}_{upload_Album.id}.jpg";

                        //        byte[] _stream = await _aWSS3Repository.GetImageStreamById(_artWorkKey);

                        //        if (_stream != null)
                        //        {
                        //            HttpStatusCode httpStatusCode = await _unitOfWork.MusicAPI.UploadArtwork(upload_Album.dh_album_id.ToString(), _stream);
                        //            if (httpStatusCode == HttpStatusCode.Created)
                        //            {
                        //                upload_Album.artwork_uploaded = true;
                        //            }
                        //            else
                        //            {
                        //                upload_Album.artwork_uploaded = false;
                        //            }
                        //            await _unitOfWork.UploadAlbum.UpdateArtworkUploaded(upload_Album);
                        //        }
                        //    }
                        //}
                        //catch (Exception)
                        //{

                        //}
                        #endregion

                        #region -----------  Create Track ------------------------------------------------------------------------------------
                        //if (upload_Tracks[i].dh_track_id == null)
                        //{
                        //    DHTrack dHTrack = await _unitOfWork.MusicAPI.CreateUploadTrack(_appSettings.Value.MasterWSId, upload_Tracks[i], upload_Album?.dh_album_id, org_User);

                        //    if (dHTrack != null)
                        //    {
                        //        upload_Tracks[i].modified = false;
                        //        upload_Tracks[i].dh_track_id = dHTrack.id;

                        //        await _unitOfWork.UploadTrack.UpdateDHTrackId(upload_Tracks[i]);
                        //    }
                        //}
                        #endregion

                        #region -----------  Sync upload assets ------------------------------------------------------------------------------------

                        if (upload_Tracks[i].dh_track_id != null && upload_Tracks[i].asset_upload_status == "S3 Success")
                        {
                            DHTrack dHTrack = await _unitOfWork.MusicAPI.GetTrackById(upload_Tracks[i].dh_track_id.ToString());
                            if (dHTrack == null)
                            {
                                upload_Tracks[i].asset_upload_status = "error";
                                upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                upload_Tracks[i].asset_uploaded = false;
                                await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                            }
                            else if (dHTrack.audio?.size > 0)
                            {
                                upload_Tracks[i].asset_upload_status = "completed";
                                upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                upload_Tracks[i].asset_uploaded = true;
                                await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                            }
                            else
                            {
                                byte[] _stream = await _aWSS3Repository.GetImageStreamById(_appSettings.Value.AWSS3.FolderName + "/" + upload_Tracks[i].s3_id);

                                if (_stream != null)
                                {
                                    HttpStatusCode httpStatusCode = await _unitOfWork.MusicAPI.UploadTrack(upload_Tracks[i].dh_track_id.ToString(), _stream);
                                    if (httpStatusCode == HttpStatusCode.Created)
                                    {
                                        upload_Tracks[i].asset_upload_status = "completed";
                                        upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                        upload_Tracks[i].asset_uploaded = true;
                                        await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);

                                        //--- Update PRS
                                        //await _ctagLogic.UpdatePRSforTrack((Guid)upload_Tracks[i].upload_id, null, c_Tags);
                                    }
                                    else
                                    {
                                        upload_Tracks[i].asset_upload_status = "error";
                                        upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                        upload_Tracks[i].asset_uploaded = false;
                                        await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                                    }
                                }
                                else
                                {
                                    upload_Tracks[i].asset_upload_status = "error";
                                    upload_Tracks[i].asset_upload_begin = DateTime.Now;
                                    upload_Tracks[i].asset_uploaded = false;
                                    await _unitOfWork.UploadTrack.UpdateUploadTrack(upload_Tracks[i]);
                                }
                            }
                        }
                        #endregion

                        #region -----------  Add to bulkupload ------------------------------------------------------------------------------------

                        //if (dhTrackId != null && upload_Tracks[i].asset_upload_status == "S3 Success")
                        //{
                        //    bulkUploadPayloads.Add(new MA_BulkUploadPayload()
                        //    {
                        //        bucket = _appSettings.Value.AWSS3.BucketName,
                        //        key = _appSettings.Value.AWSS3.FolderName + "/" + upload_Tracks[i].s3_id,
                        //        region = _appSettings.Value.AWSS3.Reagion,
                        //        trackId = dhTrackId
                        //    });
                        //}
                        #endregion

                        _logger.LogDebug(string.Format("Checked and created {0} / {1}", x++, upload_Tracks.Count()));
                    }
                } 

                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Track_Upload_Service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Track_Upload_Service.ToString(),
                    timestamp = DateTime.Now
                });

            }
            catch (Exception ex)
            {
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Track_Upload_Service,
                    status = enServiceStatus.fail.ToString(),
                    serviceName = enServiceType.Track_Upload_Service.ToString(),
                    timestamp = DateTime.Now
                });
                _logger.LogError(ex, "ProcessUploadedTracks");
            }

            #region -----------  Import Begin ------------------------------------------------------------------------------------
            //if (bulkUploadPayloads.Count() > 0)
            //{
            //    HttpWebResponse httpWebResponse = await _unitOfWork.MusicAPI.SendImportBegin(bulkUploadPayloads);

            //    if (httpWebResponse != null && httpWebResponse.StatusCode == HttpStatusCode.OK)
            //    {
            //        using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
            //        {
            //            string result = streamReader.ReadToEnd();
            //            dynamic aa = JsonConvert.DeserializeObject<dynamic>(result);
            //        }

            //        for (int i = 0; i < upload_Tracks.Count(); i++)
            //        {
            //            if (upload_Tracks[i].dh_track_id != null && upload_Tracks[i].asset_upload_status == "S3 Success")
            //            {
            //                upload_Tracks[i].asset_upload_status = "Begin";
            //                upload_Tracks[i].asset_upload_begin = DateTime.Now;
            //                await _unitOfWork.UploadTrack.UpdateTrackByStatus(upload_Tracks[i]);
            //            }
            //        }


            //        _logger.LogDebug(string.Format("Upload begin done"));
            //    }
            //}
            #endregion                     

            await Task.Delay(TimeSpan.FromSeconds(10));

            await ProcessUploadedTracks();
        }

        public async Task ReadPlayoutResponse()
        {
            try
            {
                        
                while (true)
                {                    
                    await Task.Delay(20000);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReadPlayoutResponse");
            }
        }

        public async Task PublishPlayouts()
        {
            try
            {
                _ = Task.Run(() => _playoutLogic.S3Cleanup()).ConfigureAwait(false);                 
                await _playoutLogic.ProcessPublishPlayOut();                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublishPlayouts | Module: {Module}", "Playout");
            }
        }

        public async Task CheckUploadStatus()
        {
            List<upload_track> upload_Tracks = _unitOfWork.UploadTrack.Find(a => a.asset_upload_status == "Begin" || a.asset_upload_status == "created").ToList();

            List<MA_BulkUploadPayload> bulkUploadPayloads = new List<MA_BulkUploadPayload>();
            List<string> trackIdsToBeChecked = new List<string>();

            _logger.LogDebug("Found - " + upload_Tracks.Count() + " tracks");

            for (int i = 0; i < upload_Tracks.Count(); i++)
            {
                trackIdsToBeChecked.Add(upload_Tracks[i].dh_track_id.ToString());
            }

            HttpWebResponse httpWebResponse = await _unitOfWork.MusicAPI.CheckImportStatus(trackIdsToBeChecked);

            if (httpWebResponse != null && httpWebResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var streamReader = new StreamReader(httpWebResponse.GetResponseStream()))
                {
                    string result = streamReader.ReadToEnd();
                    List<MA_BulkUploadStatus> _BulkUploadStatuses = JsonConvert.DeserializeObject<List<MA_BulkUploadStatus>>(result);

                    for (int i = 0; i < upload_Tracks.Count(); i++)
                    {
                        MA_BulkUploadStatus mA_BulkUploadStatus = _BulkUploadStatuses.FirstOrDefault(a => a.trackId == upload_Tracks[i].dh_track_id);
                        if (mA_BulkUploadStatus != null)
                        {
                            if (mA_BulkUploadStatus.status == "completed")
                            {
                                upload_Tracks[i].asset_uploaded = true;
                            }

                            if (mA_BulkUploadStatus.status == "failed")
                            {
                                upload_Tracks[i].asset_upload_status = mA_BulkUploadStatus.status + " - " + mA_BulkUploadStatus.error;
                            }
                            else
                            {
                                upload_Tracks[i].asset_upload_status = mA_BulkUploadStatus.status;
                            }
                            upload_Tracks[i].asset_upload_last_check = DateTime.Now;
                        }
                    }
                }
                await _unitOfWork.Complete();
            }
            _logger.LogDebug("Done - " + DateTime.Now.ToLongDateString());
        }

        public async Task FixCtagRules()
        {
            IEnumerable<c_tag_extended> ctag_Extended = _unitOfWork.CTagsExtended.GetAll();
            //IEnumerable<MLTrackDocument> mLTrackDocuments = await _unitOfWork.TrackOrg.GetTrackElasticByWorkspaceId(Guid.Parse("7d7d558d-22c5-4725-9bb1-c382fd971e23"));

            try
            {
                foreach (var item in ctag_Extended)
                {
                    if (item.condition != null)
                    {
                        if (item.condition.Contains("record_label"))
                        {
                            item.condition = item.condition.Replace("record_label", "recordLabel");
                            _unitOfWork.CTagsExtended.Update(item);
                            await _unitOfWork.Complete();
                        }




                        if (item.condition != null)
                        {
                            if (item.condition.Contains("extIdentifiers") || item.condition.Contains("ips"))
                            {
                                List<TagCondition> tagConditions = JsonConvert.DeserializeObject<List<TagCondition>>(item.condition);

                                for (int i = 0; i < tagConditions.Count; i++)
                                {
                                    if ((tagConditions[i].and != null && tagConditions[i].and.condition != null && !string.IsNullOrEmpty(tagConditions[i].and.property) && !string.IsNullOrEmpty(tagConditions[i].and.value)) ||
                                    (tagConditions[i].defaultItem != null && tagConditions[i].defaultItem.condition != null && !string.IsNullOrEmpty(tagConditions[i].defaultItem.property) && !string.IsNullOrEmpty(tagConditions[i].defaultItem.value)) ||
                                    (tagConditions[i].or != null && tagConditions[i].or.Count() > 0 && tagConditions[i].or[0].condition != null && !string.IsNullOrEmpty(tagConditions[i].or[0].property) && !string.IsNullOrEmpty(tagConditions[i].or[0].value)))
                                    {
                                        if (tagConditions[i].defaultItem?.property == "extIdentifiers" || tagConditions[i].defaultItem?.property == "ips")
                                        {
                                            tagConditions[i].defaultItem.property = tagConditions[i].defaultItem.innerProperty;
                                            tagConditions[i].defaultItem.innerProperty = null;
                                        }
                                        else if (tagConditions[i].and?.property == "extIdentifiers" || tagConditions[i].and?.property == "ips")
                                        {
                                            tagConditions[i].and.property = tagConditions[i].and.innerProperty;
                                            tagConditions[i].and.innerProperty = null;
                                        }
                                        else if (tagConditions[i].or?.Count() > 0 && tagConditions[i].or[0].property == "extIdentifiers" ||
                                            tagConditions[i].or?.Count() > 0 && tagConditions[i].or[0].property == "ips")
                                        {
                                            tagConditions[i].or[0].property = tagConditions[i].or[0].innerProperty;
                                            tagConditions[i].or[0].innerProperty = null;
                                        }
                                    }
                                    else
                                    {
                                        tagConditions[i] = null;
                                    }
                                }
                                tagConditions.RemoveAll(a => a == null);

                                item.condition = JsonConvert.SerializeObject(tagConditions, Formatting.Indented, new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                });
                                _unitOfWork.CTagsExtended.Update(item);
                                await _unitOfWork.Complete();
                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error " + ex);
            }

            _logger.LogDebug("Done ");
            //List<TagCondition> ruleList = new List<TagCondition>();



            //await FixCtagRules();
        }

        public string RemoveSpecialCharactorsSpacesAndSomeText_M2(string val)
        {
            if (!string.IsNullOrEmpty(val))
            {
                string[] _valList = val.ToLower().Split(' ');
                val = "";
                List<string> _ignoreWords = new List<string>();
                _ignoreWords.Add("a");
                _ignoreWords.Add("&");
                _ignoreWords.Add("an");
                _ignoreWords.Add("the");

                foreach (var item in _valList)
                {
                    if (!_ignoreWords.Contains(item))
                        val += item + " ";
                }

                val = val.Replace("'ve", " ").Replace("'s", " ").Replace("'re", " ").Replace("'t", " ").Replace("'m", " ");
                val = Regex.Replace(val, @"[^\w]+", " ");

                return Regex.Replace(val, " {2,}", " ");
            }
            else
            {
                return "";
            }
        }

       

        public async Task DailyNightTimeService()
        {
            _ = Task.Run(() => SearchableByValidFrom()).ConfigureAwait(false);
            _ = Task.Run(() => PreReleaseByValidFrom()).ConfigureAwait(false);
            _ = Task.Run(() => PRSIndex(false)).ConfigureAwait(false);
            //_ = Task.Run(() => PRSIndex2(false)).ConfigureAwait(false);
            await TakedownByValidTo();           
                      
        }

        public async Task UploadAsset()
        {
            string aaa = _aWSS3Repository.GeneratePreSignedURLForMlTrack(_appSettings.Value.AWSS3.BucketName, "BROMIS/live/20220208/100012/resources/1000111.mp3");
        }
    }
}
