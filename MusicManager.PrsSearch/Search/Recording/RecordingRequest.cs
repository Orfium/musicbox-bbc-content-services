using MusicManager.PrsSearch;
using RecordingServiceReference;
using Soundmouse.Messaging;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MusicManager.PrsSearch.Recording
{
    public abstract class RecordingRequest
    {
        private static readonly GetRecordingsSoap _client = new GetRecordingsSoapClient(GetRecordingsSoapClient.EndpointConfiguration.GetRecordingsSoap);


        public int PageSize { get; set; } = 20;

        protected abstract string RequestType { get; }

        /// <summary>
        /// Maximum number of results to return for request - if there are that many.
        /// </summary>
        protected virtual int Limit => 200;

        private string StatsKey => $"matching.search.prs-recording.by-{RequestType}";


        protected abstract NewRecordingsSearchRequest CreateRequest();

        public Models.Recording[] GetResults(string sessionId)
        {
            var request = CreateRequest();           

            request.AuthenticationToken = new AuthenticationToken
            {
                SessionId = sessionId
            };

            request.pageSize = PageSize;
            request.startRecord = 1;

            SetRequestProperties(request);

            var maxPages = (int)Math.Ceiling((double)Limit / PageSize);

            var pages = GetAllPages(request, maxPages);
           
            var recordings = pages.SelectMany(p => p.RecordingSummaryList)
                                  .Where(r => r.RecordingId.Trim('0').Length > 0)
                                  .Select(ToRecording)
                                  .ToArray();

            Stats.Increment(recordings.Any() ? $"{StatsKey}.found" : $"{StatsKey}.not-found");

            if (recordings.Length == Limit)
            {
                Serilog.Log.Logger.Debug(
                    "Search type: {Prs_SearchType}. Recording limit reached.", SearchType.Recording);
            }

            return recordings;
        }

        protected IEnumerable<WebServiceRecordingsSummariesBO> GetAllPages(NewRecordingsSearchRequest request,
                                                                           int maxPages = 5)
        {
            var response = GetPage(request);

            if (response.TotalRecdgsFound < 1)
                yield break;

            yield return response;

            var pages = (int)Math.Ceiling((float)response.TotalRecdgsFound / request.pageSize);

            pages = Math.Min(pages, maxPages);

            for (var i = 1; i < pages; i++)
            {
                var page = GetPage(request, i);

                yield return page;
            }
        }

        protected WebServiceRecordingsSummariesBO GetPage(NewRecordingsSearchRequest request,
                                                          int page = 0,
                                                          int retries = 3)
        {
            request.startRecord = 1 + page * PageSize;

            try
            {
                var response = Stats.Time(
                    () => _client.NewRecordingsSearchAsync(request).Result.NewRecordingsSearchResult,
                    $"{StatsKey}.requested");

                return response;
            }
            catch (Exception ex)
            {
                Stats.Increment($"{StatsKey}.error");

                if (retries <= 1)
                    throw new PrsServiceException(ex);

                Console.WriteLine($"Error returned by PRS web service: '{ex.Message}'. Retries remaining: {retries}.");

                return GetPage(request, page, retries - 1);
            }
        }


        private void SetRequestProperties(NewRecordingsSearchRequest request)
        {
            request.ArtistName1 = request.ArtistName1 ?? "";
            request.ArtistName2 = request.ArtistName2 ?? "";
            request.FirstRecordingOnly = request.FirstRecordingOnly ?? "";
            request.FuzzySearch = request.FuzzySearch ?? "";
            request.IncludeProdDtls = request.IncludeProdDtls ?? "";
            request.IncludeWorkDtls = request.IncludeWorkDtls ?? "";
            request.IpaId = request.IpaId ?? "";
            request.IsrcNo = request.IsrcNo ?? "";
            request.ProdCatNo = request.ProdCatNo ?? "";
            request.ProdID = request.ProdID ?? "";
            request.ProdLabel = request.ProdLabel ?? "";
            request.ProdTitle = request.ProdTitle ?? "";
            request.Title = request.Title ?? "";
            request.TrackPos = request.TrackPos ?? "";
            request.Tunecode = request.Tunecode ?? "";
        }

        private Models.Recording ToRecording(WebServiceRecordingsSummaryBO result)
        {
            var artists = new List<InterestedParty>();

            if (!string.IsNullOrEmpty(result.Artist1))
                artists.Add(new InterestedParty { FullName = HttpUtility.HtmlDecode(result.Artist1), Role = enIPRole.performer.ToString() });
            if (!string.IsNullOrEmpty(result.Artist2))
                artists.Add(new InterestedParty { FullName = HttpUtility.HtmlDecode(result.Artist2), Role = enIPRole.performer.ToString() });

            if (!string.IsNullOrEmpty(result.Writers))
            {
                if (result.Writers.Contains('/'))
                {
                    string[] composers = result.Writers.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in composers)
                    {
                        artists.Add(new InterestedParty { FullName = HttpUtility.HtmlDecode(item.Trim()), Role = enIPRole.composer.ToString() });
                    }
                }
            }

            if (result.IsrcNumber.Contains('/') || result.IsrcNumber.Contains('\\') || result.IsrcNumber.Length > 15)
            {
                Console.WriteLine(
                    $"Invalid ISRC received: {result.IsrcNumber ?? "(null)"}. (Tunecode '{result.TuneCode ?? "(null)"})'");
            }

            int recordingId;

            if (!int.TryParse(result.RecordingId, out recordingId) || recordingId < 1)
                throw new ArgumentOutOfRangeException(nameof(result.RecordingId), result.RecordingId, "must be numeric");

            var recording = new Models.Recording
            {
                AlternateTitle = result.Title2 == "" ? null : HttpUtility.HtmlDecode(result.Title2),
                Artists = artists,
                Isrc = result.IsrcNumber == "" ? null : result.IsrcNumber,
                MedleyTitle = result.MedleyTitle == "" ? null : result.MedleyTitle,
                MusicOrigin = result.Origin == "" ? null : result.Origin,
                RecordingId = recordingId,
                Title = result.Title1 == "" ? null : HttpUtility.HtmlDecode(result.Title1),
                Tunecode = result.TuneCode == "" ? null : result.TuneCode?.TrimStart('0'),
                ProductCatNo = result.ProductCatNo,
                ProductTitle = result.ProductTitle
            };

            float duration;

            if (!string.IsNullOrEmpty(result.Duration) && float.TryParse(result.Duration, out duration))
                recording.Duration = duration;

            int year;

            if (!string.IsNullOrEmpty(result.ProductionYear) && int.TryParse(result.ProductionYear, out year))
                recording.YearProduced = year;

            if (!string.IsNullOrEmpty(result.YearRecorded) && int.TryParse(result.YearRecorded, out year))
                recording.YearRecorded = year;

            return recording;
        }
    }
}