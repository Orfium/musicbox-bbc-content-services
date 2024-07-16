using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MusicManager.Logics.Logics
{
    public class ElasticLogic : IElasticLogic
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<ElasticLogic> _logger;
        private List<Guid> ChartedAlbumTrackIdList = null;

        public ElasticLogic(IOptions<AppSettings> appSettings, IElasticClient elasticClient, ILogger<ElasticLogic> logger)
        {

            _appSettings = appSettings;
            _elasticClient = elasticClient;
            _logger = logger;            
        }

        public async Task<MLTrackDocument> GetElasticTrackDocById(Guid trackDocId)
        {
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.id)
                   .Value(trackDocId)
                  )));

                return trackDocuments.Documents.FirstOrDefault();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<MLAlbumDocument> GetElasticAlbumByProdId(Guid prodId)
        {
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.prodId)
                   .Value(prodId)
                  )));

                return trackDocuments.Documents.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<bool> TrackIndex(MLTrackDocument mLTrackDocument)
        {
            IndexResponse indexResponse = await _elasticClient.IndexAsync(new IndexRequest<MLTrackDocument>(mLTrackDocument, _appSettings.Value.Elasticsearch.track_index));

            return indexResponse.IsValid;
        }

        public async Task CreateTrackIndex()
        {

            try
            {
                var createIndexResponse = await _elasticClient.Indices.CreateAsync(_appSettings.Value.Elasticsearch.track_index, c => c
                  .Settings(s => s
                     .Analysis(a => a
                         .Normalizers(aa => aa
                             .Custom("ml_normalizer", d => d.Filters("lowercase"))
                         )
                     )
                 )
                .Map<MLTrackDocument>(m => m.AutoMap())
            );
                _logger.LogDebug(createIndexResponse.ToString());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task CreateAlbumIndex()
        {
            var createIndexResponse = await _elasticClient.Indices.CreateAsync(_appSettings.Value.Elasticsearch.album_index, c => c
            .Settings(s => s
                     .Analysis(a => a
                         .Normalizers(aa => aa
                             .Custom("ml_normalizer", d => d.Filters("lowercase"))
                         )
                     ))
                .Map<MLAlbumDocument>(m => m.AutoMap())
            );
            _logger.LogDebug(createIndexResponse.ToString());
        }

        public async Task<(string error, Guid id, string reason)[]> BulkIndexAlbumDocument(List<MLAlbumDocument> albumDocuments, 
            string orgId,
            int retries = 3)
        {
            try
            {
                IEnumerable<BulkResponseItemBase> errors = null;


                var response = await _elasticClient.BulkAsync(b => b
                    .Index(_appSettings.Value.Elasticsearch.album_index) //track-ml-test
                    .IndexMany(albumDocuments, (d, t) => d.Id(t.id))
                );

                errors = response.ItemsWithErrors;

                return errors
                    .Select(i => (i.Error.Type, new Guid(i.Id), i.Error.Reason))
                    .ToArray();

            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "BulkIndexTrackDocument retrieving | Retry: {Retry} | Module: {Module}", retries, "Album index");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    await BulkIndexAlbumDocument(albumDocuments, orgId, retries - 1);
                } 
                _logger.LogError("BulkIndexAlbumDocument - " + albumDocuments[0].id, ex);
                return null;
            }
        }

        public async Task<(string error, Guid id, string reason)[]> BulkIndexTrackDocument(
            List<MLTrackDocument> trackDocuments,
            int retries = 3)
        {
            try
            {
                IEnumerable<BulkResponseItemBase> errors = null;

                var response = await _elasticClient.BulkAsync(b => b
                    .Index(_appSettings.Value.Elasticsearch.track_index) //track-ml-test
                    .IndexMany(trackDocuments, (d, t) => d.Id(t.id))
                );

                errors = response.ItemsWithErrors;

                return errors
                    .Select(i => (i.Error.Type, new Guid(i.Id), i.Error.Reason))
                    .ToArray();

            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "BulkIndexTrackDocument retrieving | Retry: {Retry} | Module: {Module}", retries, "Track index");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    await BulkIndexTrackDocument(trackDocuments,retries - 1);
                }
                _logger.LogError(ex, "BulkIndexTrackDocument");
                return null;
            }
        }

        public async Task<SearchData<List<MLTrackDocument>>> GetAllMasterTracksByAlbumId(Guid albumId, string orgId,int retries=3)
        {

            SearchData<List<MLTrackDocument>> searchObj = new SearchData<List<MLTrackDocument>>();
            try
            {

                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                    .Size(10000)
                   .Query(a => a.Term(c => c
                      .Field(p => p.prodId)
                      .Value(albumId.ToString())
                     ) && a.Bool(
                         b => b
                            .MustNot(
                                bs => bs.Term(p => p.archived, true),
                                bs => bs.Term(p => p.sourceDeleted, true)
                                )
                         )));

                searchObj.Data = trackDocuments.Documents.ToList<MLTrackDocument>();
                searchObj.TotalCount = trackDocuments.Total;
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "GetAllMasterTracksByAlbumId retrieving | Retry: {Retry} | Module: {Module}", retries, "Track index");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    await GetAllMasterTracksByAlbumId(albumId, orgId, retries - 1);
                }

                searchObj = null;               
                _logger.LogError(ex, "GetAllMasterTracksByAlbumId - ALBUM ID - " + albumId);
            }

            return searchObj;
        }

        public async Task<long> GetValidIndexCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                      b => b
                         .MustNot(
                             //bs => bs.Term(p => p.archived, true),
                             bs => bs.Term(p => p.sourceDeleted, true),
                             bs => bs.Term(p => p.musicOrigin, "live")
                             )
                      )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                          b => b
                             .MustNot(
                                 //bs => bs.Term(p => p.archived, true),
                                 bs => bs.Term(p => p.sourceDeleted, true),
                                 bs => bs.Term(p => p.musicOrigin, "live")
                                 )
                          )));
            }

            return countResponse.Count;
        }

        public async Task<long> GetArchiveIndexCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.archived, true)
                         )
                  )
                  && a.Bool(
                      b => b
                     .MustNot(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         ))
                  ));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.archived, true)
                         )
                  ) && a.Bool(
                      b => b
                     .MustNot(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         ))));
            }

            return countResponse.Count;
        }

        public async Task<long> GetArchiveAlbumIndexCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.archived, true)

                         )
                  ) && a.Bool(
                      b => b
                     .MustNot(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         ))
                  ));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.archived, true)
                         )
                  ) && a.Bool(
                      b => b
                     .MustNot(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         ))
                  ));
            }

            return countResponse.Count;
        }
        public async Task<long> GetValidAlbumIndexCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                  b => b
                     .MustNot(
                         //bs => bs.Term(p => p.archived, true),
                         bs => bs.Term(p => p.sourceDeleted, true)
                         )
                  )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                  b => b
                     .MustNot(
                         //bs => bs.Term(p => p.archived, true),
                         bs => bs.Term(p => p.sourceDeleted, true)
                         )
                  )));
            }

            return countResponse.Count;
        }

        public async Task<long> GetRestrictIndexCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.restricted, true)
                         )
                  )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.restricted, true)
                         )
                  )));
            }



            return countResponse.Count;
        }

        public async Task<long> GetSourceIndexCount(TrackCountPayload trackCountPayload)
        {

            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         )
                  )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         )
                  )));
            }

            return countResponse.Count;
        }



        public async Task<IEnumerable<MLTrackDocument>> GetTrackElasticTrackListByIds(Guid[] trackIds,int retries=3)
        {
            try
            {
                var docs = await _elasticClient.SearchAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Ids(i => i.Values(trackIds)) && q.Bool(
                b => b.MustNot(                        
                         bs => bs.Term(p => p.liveCopy, true)
                         )
                ))
                .Size(trackIds.Length));               

                return docs.Documents;
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "GetTrackElasticTrackListByIds retrieving | Retry: {Retry} | Module: {Module}", retries, "Track index");
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    await GetTrackElasticTrackListByIds(trackIds, retries - 1);
                }

                _logger.LogError(ex, "GetTrackElasticTrackListByIds. track ids {trackIds}", trackIds);
                return Enumerable.Empty<MLTrackDocument>();
            }
        }

        public async Task RestraictTracks(Guid[] trackIds, bool restrict)
        {
            IEnumerable<MLTrackDocument> MLTrackDocuments = await GetTrackElasticTrackListByIds(trackIds);
            List<MLTrackDocument> mLTrackDocuments = MLTrackDocuments?.Select(c => { c.restricted = restrict; return c; }).ToList();
            var asyncIndexResponse = await BulkIndexTrackDocument(mLTrackDocuments);
        }

        public async Task ArchiveTracks(Guid[] trackIds, bool archived)
        {
            IEnumerable<MLTrackDocument> MLTrackDocuments = await GetTrackElasticTrackListByIds(trackIds);
            List<MLTrackDocument> mLTrackDocuments = MLTrackDocuments?.Select(c => { c.archived = archived; return c; }).ToList();
            var asyncIndexResponse = await BulkIndexTrackDocument(mLTrackDocuments);
        }

        public async Task<List<MLTrackDocument>> SearchTracksForIndexCtags(int size, Guid indexedCtagIdetifier)
        {
            var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                   .Size(size)
                  .Query(a => a.Bool(
                        b => b
                           .MustNot(
                               bs => bs.Term(p => p.indexed_ctag_idetifier, indexedCtagIdetifier),
                               bs => bs.Term(p => p.archived, true),
                               bs => bs.Term(p => p.sourceDeleted, true),
                               bs => bs.Term(p => p.musicOrigin, "live")
                               )
                        )).Sort(q => q.Descending(a => a.dateLastEdited)));

            return trackDocuments.Documents.ToList<MLTrackDocument>();
        }

        public async Task<long> GetIndexCtagCompletedCount(TrackCountPayload trackCountPayload, c_tag_index_status cTagIndexStatus)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Match(m => m
                        .Field(p => p.wsId)
                        .Query(trackCountPayload.refId)
                    )
                    && q.Bool(
                    b => b
                     .MustNot(
                         bs => bs.Term(p => p.archived, true),
                         bs => bs.Term(p => p.sourceDeleted, true),
                         bs => bs.Term(p => p.musicOrigin, "live")
                         )
                 ) && q.Bool(b => b.Must(m => m.Term(t => t.indexed_ctag_idetifier, cTagIndexStatus.update_idetifier)))));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.DisMax(dm => dm
                    .Queries(dq => dq
                    .Match(m => m
                        .Field(p => p.libId)
                        .Query(trackCountPayload.refId)
                    ), dq => dq
                    .Match(m => m
                        .Field(p => p.indexed_ctag_idetifier)
                        .Query(cTagIndexStatus.update_idetifier.ToString())
                    ), dq => dq))
                    && q.Bool(
                    b => b
                     .MustNot(
                         bs => bs.Term(p => p.archived, true),
                         bs => bs.Term(p => p.sourceDeleted, true),
                         bs => bs.Term(p => p.musicOrigin, "live")
                         )
                 )));
            }
            return countResponse.Count;
        }

        public async Task<bool> UpdateIndex(MLTrackDocument mLTrackDocument)
        {
            MLTrackDocument currentIndex = await GetElasticTrackDocById(mLTrackDocument.id);

            if (currentIndex == null)
                return false;

            mLTrackDocument.wsName = currentIndex.wsName;
            mLTrackDocument.libName = currentIndex.libName;
            mLTrackDocument.wsType = currentIndex.wsType;
            mLTrackDocument.wsName = currentIndex.wsName;
            mLTrackDocument.wsName = currentIndex.wsName;
            mLTrackDocument.assets = currentIndex.assets;
            mLTrackDocument.duration = currentIndex.duration;
            mLTrackDocument.arid = currentIndex.arid;

            return await TrackIndex(mLTrackDocument);
        }

        public async Task<List<MLTrackDocument>> SearchTracksForPRSIndex(int size)
        {
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Size(size)
                .QueryOnQueryString($"musicOrigin:commercial AND (!_exists_:prsFound) AND sourceDeleted:false AND (!prsSearchError:true)") // $"musicOrigin:commercial AND (!_exists_:prsFound) AND sourceDeleted:false AND (!prsSearchError:true)"
                .Sort(q => q.Descending(a => a.dateLastEdited)));

                return trackDocuments.Documents.ToList();
            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError(ex,"SearchTracksForPRSIndex");
                return null;
            }
        }

        public async Task<List<MLTrackDocument>> GetPRSNotfoundListByPrssearchDateRange(int size, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Size(size)
                .Query(q => q.Bool(
                b => b.Must(m => m
                        .QueryString(qs => qs.Query("prsFound:false"))
                && q.Bool(b => b.Must(m.DateRange(r => r.Field(p => p.prsSearchDateTime).Format("yyyy-M-d").GreaterThanOrEquals(fromDate.ToString("yyyy-MM-dd")).LessThanOrEquals(toDate.ToString("yyyy-MM-dd")))))))));

                return trackDocuments.Documents.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<MLTrackDocument>> SearchTracksForPRSIndex2(int size)
        {
            try
            {              

                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Size(size)
                .QueryOnQueryString($"musicOrigin:commercial AND (!_exists_:prsFound) AND sourceDeleted:false")
                .Sort(q => q.Ascending(a => a.dateLastEdited)));

                return trackDocuments.Documents.ToList();
            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError("SearchTracksForPRSIndex", ex);
                return null;
            }
        }

        public async Task<List<MLTrackDocument>> GetChartedPRSNotCheckedTracks(int size)
        {
            try
            {
                List<MLTrackDocument> mLTrackDocuments = new List<MLTrackDocument>();

                //--- Get Charted tracks

                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
               .Size(size)
               .Query(q => q
                 .Bool(b => b
                     .Must(m => m
                         .QueryString(qs => qs.Query("charted:true AND (!_exists_:prsFound)  AND sourceDeleted:false"))
                 ))
               ).Sort(q => q.Descending(a => a.dateLastEdited)));

                mLTrackDocuments.AddRange(trackDocuments.Documents.ToList());

                return mLTrackDocuments;
            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError("GetChartedPRSNotCheckedTracks", ex);
                return null;
            }
        }

        public async Task<List<MLTrackDocument>> GetCommercialPRSNotCheckedTracks(int size)
        {
            try
            {
                List<MLTrackDocument> mLTrackDocuments = new List<MLTrackDocument>();

                //--- Get Charted tracks

                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
               .Size(size)
               .Query(q => q
                 .Bool(b => b
                     .Must(m => m
                         .QueryString(qs => qs.Query("musicOrigin:commercial AND (!_exists_:prsFound)  AND sourceDeleted:false"))
                 ))
               ).Sort(q => q.Descending(a => a.dateLastEdited)));

                mLTrackDocuments.AddRange(trackDocuments.Documents.ToList());

                return mLTrackDocuments;
            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError("GetChartedPRSNotCheckedTracks", ex);
                return null;
            }
        }

        public async Task<List<MLTrackDocument>> GetChartedPRSNotCheckedAlbumTracks(int size)
        {
            try
            {

                if (ChartedAlbumTrackIdList != null && ChartedAlbumTrackIdList.Count() > 0)
                    return await GetPRSToBesearchedTracksByIds(size);


                if (ChartedAlbumTrackIdList == null)
                {
                    ChartedAlbumTrackIdList = new List<Guid>();

                    List<MLTrackDocument> mLTrackDocuments = new List<MLTrackDocument>();

                    //--- Get Charted tracks
                    var albumDocuments = await _elasticClient.SearchAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                   .Size(8000)
                   .Query(q => q
                     .Bool(b => b
                         .Must(m => m
                             .QueryString(qs => qs.Query("charted:true AND sourceDeleted:false"))  
                     ))
                   ).Sort(q => q.Descending(a => a.dateLastEdited)));

                    foreach (MLAlbumDocument item in albumDocuments.Documents.ToList())
                    {
                        #region ---- Get the count
                        //var trackCount = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                        //.QueryOnQueryString($"prodId:{item.prodId} AND (!_exists_:prsFound)"));

                        //count += trackCount.Count;

                        //Console.WriteLine($"track count - {count}");
                        #endregion

                        var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                        .QueryOnQueryString($"prodId:{item.prodId} AND (!_exists_:prsFound) AND sourceDeleted:false"));


                        if (trackDocuments.Documents.Count() > 0)
                        {
                            ChartedAlbumTrackIdList.AddRange(trackDocuments.Documents.Select(a => a.id));                            

                            _logger.LogDebug($"Track counts - {ChartedAlbumTrackIdList.Count()}");
                            //mLTrackDocuments.AddRange(trackDocuments.Documents);
                        }


                        //if (mLTrackDocuments.Count() > 500)
                        //    break;                
                    }

                    //foreach (var id in ChartedAlbumTrackIdList)
                    //{
                    //    Console.WriteLine(id);
                    //}

                    _logger.LogInformation($"Chart album tracks to be searched - {ChartedAlbumTrackIdList.Count()}");

                    if (ChartedAlbumTrackIdList.Count() > 0)
                        return await GetPRSToBesearchedTracksByIds(size);
                }

                return new List<MLTrackDocument>();
            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError("GetChartedPRSNotCheckedAlbumTracks", ex);
                return null;
            }
        }


        public async Task<List<MLTrackDocument>> GetPRSToBesearchedTracksByIds(int size)
        {
            try
            {
                List<Guid> trackIdList = new List<Guid>();

                _logger.LogInformation($"Chart album tracks Remaining count - {ChartedAlbumTrackIdList.Count()}");

                if (ChartedAlbumTrackIdList.Count() > size)
                {
                    trackIdList = ChartedAlbumTrackIdList.GetRange(0, size);
                    ChartedAlbumTrackIdList.RemoveRange(0, size);
                }
                else {
                    trackIdList = ChartedAlbumTrackIdList;
                    ChartedAlbumTrackIdList = new List<Guid>();
                }

                var docs = await _elasticClient.SearchAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Ids(i => i.Values(trackIdList)) && q.Bool(
                b => b.Must(m => m
                        .QueryString(qs => qs.Query("(!_exists_:prsFound) AND sourceDeleted:false"))
                )))
                .Size(size));

                return docs.Documents.ToList();
            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError("GetPRSToBesearchedTracksByIds", ex);
                return null;
            }
        }

        public async Task DeleteTracks(Guid[] trackIds)
        {
            IEnumerable<MLTrackDocument> MLTrackDocuments = await GetTrackElasticTrackListByIds(trackIds);
            List<MLTrackDocument> mLTrackDocuments = MLTrackDocuments?.Select(c => { c.archived = true; c.sourceDeleted = true; c.takedownDate = DateTime.Now; return c; }).ToList();
            var asyncIndexResponse = await BulkIndexTrackDocument(mLTrackDocuments);
        }

        public async Task<bool> AlbumIndex(MLAlbumDocument mLAlbumDocument)
        {
            IndexResponse indexResponse = await _elasticClient.IndexAsync(new IndexRequest<MLAlbumDocument>(mLAlbumDocument, _appSettings.Value.Elasticsearch.album_index));

            return indexResponse.IsValid;
        }

        public async Task<List<MLTrackDocument>> SearchCtagedDocs(int size)
        {
            try
            {

                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                      .Size(size)
                     .Query(a => a.Bool(
                           b => b
                              .Must(
                                  bs => bs.Exists(a => a.Field(f => f.prsWorkTunecode))
                                  ).MustNot(
                                bs => bs.Term(p => p.musicOrigin, "live")
                               )
                           )).Sort(q => q.Descending(a => a.dateLastEdited)));

                return trackDocuments.Documents.ToList<MLTrackDocument>();
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        public async Task RestoreTracks(Guid[] trackIds, bool archived)
        {
            IEnumerable<MLTrackDocument> MLTrackDocuments = await GetTrackElasticTrackListByIds(trackIds);
            List<MLTrackDocument> mLTrackDocuments = MLTrackDocuments?.Select(c => { c.archived = false; return c; }).ToList();
            var asyncIndexResponse = await BulkIndexTrackDocument(mLTrackDocuments);
        }

        public async Task<long> GetSourceDeletedAlbumIndexCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         )
                  )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.sourceDeleted, true)
                         )
                  )));
            }

            return countResponse.Count;
        }

        public async Task<long> GetRestrictAlbumIndexCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.restricted, true)
                         )
                  )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLAlbumDocument>(c => c.Index(_appSettings.Value.Elasticsearch.album_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(
                  b => b
                     .Must(
                         bs => bs.Term(p => p.restricted, true)
                         )
                  )));
            }

            return countResponse.Count;
        }

        public async Task<MLTrackDocument> GetElasticTrackDocByIdForPlayout(Guid trackDocId)
        {
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.id)
                   .Value(trackDocId)
                  ) && a.Bool(
                  b => b
                     .MustNot(                        
                         bs => bs.Term(p => p.liveCopy, true)
                         )
                  )));

                return trackDocuments.Documents.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetElasticTrackDocByIdForPlayout | TrackId: {TrackId}",trackDocId);
                return null;
            }
        }

        public async Task<List<ServiceLog>> GetServiceStatus()
        {

            List<ServiceLog> logs = new List<ServiceLog>();
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<ServiceLog>(c => c.Index(_appSettings.Value.Elasticsearch.service_log_index));
                logs = trackDocuments.Documents.ToList<ServiceLog>();
            }
            catch (Exception ex)
            {
                logs = null;
                _logger.LogError("GetServiceStatus", ex);
            }

            return logs;
        }

        public async Task<long> GetPRSIndexedCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(b => b.Must(bm => bm.Term(t => t.prsFound, true))) && a.Bool(
                      b => b
                         .MustNot(
                             bs => bs.Term(p => p.sourceDeleted, true)
                             )
                      )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(b => b.Must(bm => bm.Term(t => t.prsFound, true))) && a.Bool(
                      b => b
                         .MustNot(
                             bs => bs.Term(p => p.sourceDeleted, true)
                             )
                      )));
            }

            return countResponse.Count;
        }

        public async Task<long> GetPRSNotMatchedCount(TrackCountPayload trackCountPayload)
        {
            CountResponse countResponse = new CountResponse();

            if (trackCountPayload.type == enWorkspaceLib.ws.ToString())
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.wsId)
                   .Value(trackCountPayload.refId)
                  ) && a.Bool(b => b.Must(bm => bm.Term(t => t.prsFound, false))) && a.Bool(
                      b => b
                         .MustNot(
                             bs => bs.Term(p => p.sourceDeleted, true)
                             )
                      )));
            }
            else
            {
                countResponse = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                    .Query(a => a.Term(c => c
                       .Field(p => p.libId)
                       .Value(trackCountPayload.refId)
                      ) && a.Bool(b => b.Must(bm => bm.Term(t => t.prsFound, false))) && a.Bool(
                      b => b
                         .MustNot(
                             bs => bs.Term(p => p.sourceDeleted, true)
                             )
                      )));
            }

            return countResponse.Count;
        }

        public async Task<IEnumerable<MLTrackDocument>> SearchTracksForPRSIndex(Guid[] trackIds)
        {
            try
            {
                var docs = await _elasticClient.SearchAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Terms(c => c
            .Field(p => p.dhTrackId)
            .Terms<Guid>(trackIds)) && q.Bool(
                b => b.MustNot(
                        bs => bs.Exists(a => a.Field(f => f.prsFound)),                        
                         bs => bs.Term(p => p.liveCopy, true)
                         )
                ))
                .Size(trackIds.Length));

                return docs.Documents;

            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError("SearchTracksForPRSIndex(trackIds)", ex);
                return null;
            }
        }

        public async Task<MLTrackDocument> GetElasticTrackDocByDhTrackId(Guid trackDocId)
        {
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(a => a.Term(c => c
                   .Field(p => p.dhTrackId)
                   .Value(trackDocId)
                  )));

                return trackDocuments.Documents.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<bool> ResetServiceLogger()
        {
            try
            {
                var result = await _elasticClient.DeleteByQueryAsync<dynamic>(c => c.Index(_appSettings.Value.Elasticsearch.service_log_index)
                .Query(q=>q.QueryString(qs=>qs.Query("*")))
                );                
                return result.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError("ResetServiceLogger", ex);
                return false;
            }

        }

        public async Task<IEnumerable<MLTrackDocument>> SearchTakedownTracks(int size)
        {
            try
            {
                var docs = await _elasticClient.SearchAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.Field(p => p.sourceDeleted).Value(false)) &&
                    f.DateRange(r => r.Field(p => p.validTo).LessThanOrEquals(DateTime.Now))))).Size(size));

                return docs.Documents;

            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError(ex, "SearchTakedownTracks");
                return null;
            }
        }        

        public async Task<long> GetTakedownTracksCount()
        {
            try
            {
                var count = await _elasticClient.CountAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.Field(p => p.sourceDeleted).Value(false)) &&
                    f.DateRange(r => r.Field(p => p.validTo).LessThanOrEquals(DateTime.Now))))));

                return count.Count;
            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError(ex, "GetTakedownTracksCount");
                return 0;
            }
        }

        public async Task<IEnumerable<MLTrackDocument>> SearchPreReleaseTracks(int size)
        {
            try
            {
                var docs = await _elasticClient.SearchAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
               .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.preRelease, false) && f.Term(t => t.archived, false) &&
                   f.DateRange(r => r.Field(p => p.validFrom).GreaterThan(DateTime.Now))))).Size(size));               

                return docs.Documents;

            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError(ex, "SearchPreReleaseTracks");
                return null;
            }
        }

        public async Task<IEnumerable<MLTrackDocument>> SearchNotPreReleaseTracks(int size)
        {
            try
            {
                // fileter not archived
                var docs = await _elasticClient.SearchAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.preRelease, true) && f.Term(t => t.archived, false) &&
                    f.DateRange(r => r.Field(p => p.validFrom).LessThanOrEquals(DateTime.Now))))).Size(size));

                return docs.Documents;

            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError(ex, "SearchNotPreReleaseTracks");
                return null;
            }
        }

        public async Task<long> GetPreReleaseTracksCount()
        {
            try
            {
                var docs = await _elasticClient.CountAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.preRelease, false) && f.Term(t => t.archived, false) &&
                    f.DateRange(r => r.Field(p => p.validFrom).GreaterThan(DateTime.Now))))));

                return docs.Count;

            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError(ex, "SearchPreReleaseTracks");
                return 0;
            }
        }

        public async Task<long> GetNotPreReleaseTracksCount()
        {
            try
            {
                var docs = await _elasticClient.CountAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.preRelease, true) && f.Term(t => t.archived, false) &&
                    f.DateRange(r => r.Field(p => p.validFrom).LessThanOrEquals(DateTime.Now))))));

                return docs.Count;

            }
            catch (UnexpectedElasticsearchClientException ex)
            {
                _logger.LogError(ex, "SearchNotPreReleaseTracks");
                return 0;
            }
        }

        public async Task DeleteAlbum(Guid albumId)
        {
            MLAlbumDocument mLAlbumDocument = await GetElasticAlbumByProdId(albumId);

            if (mLAlbumDocument != null) {
                mLAlbumDocument.sourceDeleted = true;
                mLAlbumDocument.archived = true;
                mLAlbumDocument.dateLastEdited = DateTime.Now;

                var asyncIndexResponse = await BulkIndexAlbumDocument(new List<MLAlbumDocument>() { mLAlbumDocument }, "");
            }            
        }

        public async Task<long> GetTrackCountByQuery(string query)
        {
            try
            {
                var countResponce = await _elasticClient.CountAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
             .QueryOnQueryString(query));
                return countResponce.Count;
            }
            catch (Exception)
            {
                return 0;
            }

        }

        public async Task<long> GetTrackCountByQueryAndDate(string query, DateTime date)
        {
            try
            {                
                var countResponce = await _elasticClient.CountAsync<MLTrackDocument>(s => s.Index(_appSettings.Value.Elasticsearch.track_index)
                .Query(q => q.Bool(
                b => b.Must(m => m
                        .QueryString(qs => qs.Query(query))
                && q.Bool(b=>b.Must(m.DateRange(r => r.Field(p => p.dateLastEdited).Format("yyyy-M-d").GreaterThanOrEquals(date.ToString("yyyy-MM-dd")).LessThanOrEquals(date.ToString("yyyy-MM-dd")))))))));
             
                return countResponce.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<IEnumerable<MLTrackDocument>> SearchLiveTracks()
        {
            try
            {
                var trackDocuments = await _elasticClient.SearchAsync<MLTrackDocument>(c => c.Index(_appSettings.Value.Elasticsearch.track_index)
                .QueryOnQueryString("(musicOrigin:live AND liveCopy:true)").Size(10000));

                return trackDocuments.Documents;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

}
