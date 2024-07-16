using ProductsServiceReference;
using Soundmouse.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MusicManager.PrsSearch.Product
{
    public abstract class ProductRequest
    {
        private static readonly GetProductsSoap _client = new GetProductsSoapClient(GetProductsSoapClient.EndpointConfiguration.GetProductsSoap);

        public int PageSize { get; set; } = 20;

        public virtual int Limit => 1000;


        protected abstract NewProductsSearchRequest CreateRequest();

        public Soundmouse.Messaging.Model.Track[] GetResults(string sessionId)
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

            var products = pages.SelectMany(p => p.ProductSummaryList)
                                .Where(p => !string.IsNullOrEmpty(p.Title1))
                                .Select(ToProduct)
                                .ToArray();

            //---- Don't delete this comment - UDYOGA

            //Stats.Increment(products.Any()
            //    ? "matching.search.prs-product.found"
            //    : "matching.search.prs-product.not-found");

            return products;
        }

        protected IEnumerable<WebServiceProductSummariesBO> GetAllPages(NewProductsSearchRequest request,
                                                                        int maxPages = 5)
        {
            var response = GetPage(request);

            if (response.TotalRecdgsFound < 1)
                yield break;

            yield return response;

            var pages = (int) Math.Ceiling((float) response.TotalRecdgsFound/request.pageSize);

            pages = Math.Min(pages, maxPages);

            for (var i = 1; i < pages; i++)
            {
                var page = GetPage(request, i);

                yield return page;
            }
        }

        protected WebServiceProductSummariesBO GetPage(NewProductsSearchRequest request,
                                                       int page = 0,
                                                       int retries = 3)
        {
            request.startRecord = 1 + page*PageSize;

            try
            {
                var response = Stats.Time(() => _client.NewProductsSearchAsync(request).Result.NewProductsSearchResult,
                    "matching.search.prs-product.requested");

                Stats.Increment("matching.search.prs-product.searched");

                return response;
            }
            catch (Exception ex)
            {
                Stats.Increment("matching.search.prs-product.error");

                if (retries <= 1)
                    throw new PrsServiceException(ex);

                Console.WriteLine($"Error received from PRS web service. Retries remaining: {retries}.");

                return GetPage(request, page, retries - 1);
            }
        }

        private void SetRequestProperties(NewProductsSearchRequest request)
        {
            // all request properties need a value or the prs service will throw an error
            request.ArtistName = request.ArtistName ?? "";
            request.CatalogueNumber = request.CatalogueNumber ?? "";
            request.FuzzySearch = request.FuzzySearch ?? "";
            request.IncludeCompilations = request.IncludeCompilations ?? "";
            request.ProductID = request.ProductID ?? "";
            request.RecordingID = request.RecordingID ?? "";
            request.Title = request.Title ?? "";
            request.Tunecode = request.Tunecode ?? "";
        }

        private Soundmouse.Messaging.Model.Track ToProduct(WebServiceProductSummaryBO result)
        {
            var track = new Soundmouse.Messaging.Model.Track()
            {
                TrackData = new Soundmouse.Messaging.Model.TrackData()
                {                   
                    Product = new Soundmouse.Messaging.Model.Product() { 
                        Identifiers = new Dictionary<string,string>()
                    }
                }
            };
            var product = new Models.Product();
            product.arg = result;

            //---- Don't delete this comment - UDYOGA

            product.Artist = HttpUtility.HtmlDecode(result.Artist1);

            product.CatalogueNumbers = new[]
            {
                HttpUtility.HtmlDecode(result.CatalogueNumber1),
                HttpUtility.HtmlDecode(result.CatalogueNumber2),
                HttpUtility.HtmlDecode(result.CatalogueNumber3),
                HttpUtility.HtmlDecode(result.CatalogueNumber4),
                HttpUtility.HtmlDecode(result.CatalogueNumber5),
            }.Where(cat => !string.IsNullOrEmpty(cat)).Distinct().ToArray();

            if (!string.IsNullOrEmpty(result.CompilationFlag))
                product.IsCompilation = result.CompilationFlag == "Y";

            product.Label = result.Label;

            int productId;

            if (!int.TryParse(result.ProductID, out productId) || productId < 1)
                throw new ArgumentOutOfRangeException(nameof(result.ProductID), result.ProductID, "must be numeric");

            product.ProductId = productId;
            product.RecordCompany = result.RecordCompany;
            product.Title = HttpUtility.HtmlDecode(result.Title1);

            return product.ToTrack(track);
        }
    }
}