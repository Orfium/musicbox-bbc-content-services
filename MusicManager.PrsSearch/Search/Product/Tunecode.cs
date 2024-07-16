using ProductsServiceReference;
using System;

namespace MusicManager.PrsSearch.Product
{
    public class Tunecode : ProductRequest
    {
        public string Value { get; set; }

        protected override NewProductsSearchRequest CreateRequest()
        {
            return new NewProductsSearchRequest
            {
                ipType = ProductEnquiryType.Tunecode,
                Tunecode = Value
            };
        }


        public static Soundmouse.Messaging.Model.Track[] GetProducts(string prsToken, string tunecode)
        {
            if (string.IsNullOrEmpty(tunecode))
                throw new ArgumentException(nameof(tunecode), tunecode);

            var search = new Tunecode {Value = tunecode};

            var results = search.GetResults(prsToken);

            PrsLogger.LogSearchPerformed(SearchType.Product, "tunecode", results.Length, tunecode);

            return results;
        }
    }
}