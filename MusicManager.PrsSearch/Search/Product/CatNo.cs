using Microsoft.Extensions.Logging;
using ProductsServiceReference;
using System;

namespace MusicManager.PrsSearch.Product
{
    public class CatNo : ProductRequest
    {
        public string Value { get; set; }       

        protected override NewProductsSearchRequest CreateRequest()
        {
            return new NewProductsSearchRequest
            {
                ipType = ProductEnquiryType.Catalogue_Number,
                CatalogueNumber = Value
            };
        }


        public static Soundmouse.Messaging.Model.Track[] GetProducts(string prsToken,string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (value.Length == 0)
                throw new ArgumentException("must not be empty", nameof(value));
            if (value.Length > 50)
                throw new ArgumentException($"length must not be greater than 50 (value: '{value}')", nameof(value));

            var search = new CatNo {Value = value};

            var results = search.GetResults(prsToken);

            return results;
        }
    }
}