using ProductsServiceReference;
using System;

namespace MusicManager.PrsSearch.Product
{
    public class RecordingId : ProductRequest
    {
        public int Value { get; set; }

        protected override NewProductsSearchRequest CreateRequest()
        {
            return new NewProductsSearchRequest
            {
                ipType = ProductEnquiryType.Recording_ID,
                RecordingID = Value.ToString()
            };
        }


        public static Soundmouse.Messaging.Model.Track[] GetProducts(string prsToken, int value)
        {
            if (value < 0 || value > 999999999)
                throw new ArgumentOutOfRangeException(nameof(value), value, "recording id invalid");

            var search = new RecordingId {Value = value};

            var results = search.GetResults(prsToken);

            PrsLogger.LogSearchPerformed(SearchType.Product, "recording_id", results.Length, value.ToString());

            return results;
        }
    }
}