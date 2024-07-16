using MusicManager.PrsSearch;
using Soundmouse.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WorksServiceReference;

namespace Soundmouse.Matching.Prs.Search.Work
{
    public abstract class WorkRequest
    {
        private static readonly GetWorksSoap _client = new GetWorksSoapClient(GetWorksSoapClient.EndpointConfiguration.GetWorksSoap);

        public int PageSize { get; set; } = 20;

        public virtual int Limit => 20;


        protected abstract NewWorkSearchRequest CreateRequest();

        public MusicManager.PrsSearch.Models.Work[] GetResults(string sessionId)
        {
            var request = CreateRequest();            

            request.AuthenticationToken = new AuthenticationToken
            {
                SessionId = sessionId
            };

            request.pageSize = PageSize;
            request.startRecord = 1;

            SetRequestProperties(request);

            var maxPages = (int) Math.Ceiling((double) Limit/PageSize);

            var pages = GetAllPages(request, maxPages);

            var works = pages.SelectMany(p => p.WorkSummaryList)
                             .Select(w => ToWork(w))
                             .ToArray();

            if (works.Any(w => string.IsNullOrEmpty(w.arg2.Tunecode)))
            {
                Serilog.Log.Logger.Warning("Search type: {Prs_SearchType}. Discarding works with no tunecode.",
                    SearchType.Work);

                works = works.Where(w => !string.IsNullOrEmpty(w.Tunecode)).ToArray();
            }

            Stats.Increment(works.Any()
                ? "matching.search.prs-work.found"
                : "matching.search.prs-work.not-found");

            return works;
        }

        protected IEnumerable<WebServiceWorkSummariesBO> GetAllPages(NewWorkSearchRequest request, int maxPages = 5)
        {
            var response = GetPage(request);

            yield return response;

            var pages = (int) Math.Ceiling((float) response.TotalRecdgsFound/request.pageSize);

            pages = Math.Min(pages, maxPages);

            for (var i = 1; i < pages; i++)
            {
                var page = GetPage(request, i);

                yield return page;
            }
        }

        protected WebServiceWorkSummariesBO GetPage(NewWorkSearchRequest request,
                                                    int page = 0,
                                                    int retries = 3)
        {
            request.startRecord = 1 + page*PageSize;

            try
            {
                var response = Stats.Time(() => _client.NewWorkSearchAsync(request).Result.NewWorkSearchResult,
                    "matching.search.prs-work.requested");

                Stats.Increment("matching.search.prs-work.searched");

                return response;
            }
            catch (Exception ex)
            {
                Stats.Increment("matching.search.prs-work.error");

                if (retries <= 1)
                    throw new PrsServiceException(ex);

                Console.WriteLine($"Error received from PRS web service. Retries remaining: {retries}.");

                return GetPage(request, page, retries - 1);
            }
        }

        private void SetRequestProperties(NewWorkSearchRequest request)
        {
            // all request properties need a value or the prs service will throw an error
            request.FuzzySearch = request.FuzzySearch ?? "";
            request.IncludeArtists = request.IncludeArtists ?? "Y";
            request.ISWC = request.ISWC ?? "";
            request.LibCatNo = request.LibCatNo ?? "";
            request.PublisherCAE1 = request.PublisherCAE1 ?? "";
            request.PublisherCAE2 = request.PublisherCAE2 ?? "";
            request.PublisherCAE3 = request.PublisherCAE3 ?? "";
            request.PublisherCAE4 = request.PublisherCAE4 ?? "";
            request.PublisherCAE5 = request.PublisherCAE5 ?? "";
            request.PublisherName1 = request.PublisherName1 ?? "";
            request.PublisherName2 = request.PublisherName2 ?? "";
            request.PublisherName3 = request.PublisherName3 ?? "";
            request.PublisherName4 = request.PublisherName4 ?? "";
            request.PublisherName5 = request.PublisherName5 ?? "";
            request.title = request.title ?? "";
            request.TrackPos = request.TrackPos ?? "";
            request.Tunecode = request.Tunecode ?? "";
            request.WriterCAE1 = request.WriterCAE1 ?? "";
            request.WriterCAE2 = request.WriterCAE2 ?? "";
            request.WriterCAE3 = request.WriterCAE3 ?? "";
            request.WriterCAE4 = request.WriterCAE4 ?? "";
            request.WriterCAE5 = request.WriterCAE5 ?? "";
            request.WriterName1 = request.WriterName1 ?? "";
            request.WriterName2 = request.WriterName2 ?? "";
            request.WriterName3 = request.WriterName3 ?? "";
            request.WriterName4 = request.WriterName4 ?? "";
            request.WriterName5 = request.WriterName5 ?? "";
        }

        private MusicManager.PrsSearch.Models.Work ToWork(WebServiceWorkSummaryBO arg, bool fullDetail = true)
        {
            var work = new MusicManager.PrsSearch.Models.Work();
            work.arg2 = arg;


            //---- Don't delete this comment - UDYOGA

            //if (arg.IswcArray.Contains('/') || arg.IswcArray.Contains('\\') || arg.IswcArray.Length > 15)
            //{
            //    throw new ApplicationException();
            //}

            //work.Iswc =
            //    arg.IswcArray.Length > 0
            //        ? new[] {arg.IswcArray}
            //        : new string[0];

            //work.LibraryCatalogueNumbers =
            //    arg.LibraryCatNo.Length > 0
            //        ? new[] {arg.LibraryCatNo}
            //        : new string[0];

            //work.Publishers = arg.PublisherArray.Select(
            //    p => new Soundmouse.Messaging.Model.InterestedParty(HttpUtility.HtmlDecode(p), "publisher")).ToArray();

            //work.Title = HttpUtility.HtmlDecode(arg.Title);
            //work.Tunecode = arg.Tunecode?.TrimStart('0');

            //if (arg.WorkType.Length > 0)
            //    work.Type = arg.WorkType;

            //work.Writers = arg.WriterArray.Select(
            //    w => new Soundmouse.Messaging.Model.InterestedParty(HttpUtility.HtmlDecode(w), "composer")).ToArray();

            //if (fullDetail)
            //    work.GetDetail();

            return work;
        }
    }
}