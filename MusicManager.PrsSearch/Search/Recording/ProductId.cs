
using MusicManager.PrsSearch;
using RecordingServiceReference;
using System;

namespace MusicManager.PrsSearch.Recording
{
    public class ProductId : RecordingRequest
    {
        public string Value { get; set; }

        protected override string RequestType { get; } = "product";


        protected override NewRecordingsSearchRequest CreateRequest()
        {
            return new NewRecordingsSearchRequest
            {
                ipType = RecordingInterestedPartyType.Product_ID,
                ProdID = Value
            };
        }


        public static Models.Recording[] GetRecordings(string prsToken, string productId)
        {
            if (productId == null)
                throw new ArgumentNullException(nameof(productId));
            if (productId.Length == 0)
                throw new ArgumentException("must not be empty", nameof(productId));

            var search = new ProductId {Value = productId};
            
            var results = search.GetResults(prsToken);

            PrsLogger.LogSearchPerformed(SearchType.Recording, "product_id", results.Length, productId);

            return results;
        }
    }
}