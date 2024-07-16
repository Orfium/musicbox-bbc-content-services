using ProductsServiceReference;
using System;

namespace MusicManager.PrsSearch.Product
{
    public class Title : ProductRequest
    {
        public const int MaxLength = 100;

        public string Value { get; set; }

        public override int Limit => 100;


        protected override NewProductsSearchRequest CreateRequest()
        {
            return new NewProductsSearchRequest
            {
                Title = Value,
                ipType = ProductEnquiryType.Title
            };
        }


        public static Soundmouse.Messaging.Model.Track[] GetProducts(string prsToken, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length == 0)
                throw new ArgumentException("must not be empty", nameof(value));
            if (value.Length > MaxLength)
                throw new ArgumentException($"length must not be greater than {MaxLength} (value: '{value}')",
                    nameof(value));

            var search = new Title {Value = value};

            var results = search.GetResults(prsToken);

            PrsLogger.LogSearchPerformed(SearchType.Product, "title", results.Length, value);

            return results;
        }
    }
}