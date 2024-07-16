using MusicManager.Application.WebService;
using MusicManager.PrsSearch.Product;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Infrastructure.WebService
{
    public class Product: IProduct
    {
        private readonly PrsSearch.PrsAuth.IAuthentication _authentication;
        public Product(
            PrsSearch.PrsAuth.IAuthentication authentication)
        {
            _authentication = authentication;
        }
        public Track[] GetProductByCatNo(string catNo)
        {
            string token = _authentication.GetSessionToken();
            var products = CatNo.GetProducts(token,catNo);
            return products;
        }

        public Track[] GetProductByRecordingId(int recordingId)
        {
            string token = _authentication.GetSessionToken();
            var products = RecordingId.GetProducts(token, recordingId);
            return products;
        }

        public Track[] GetProductByTitle(string title)
        {
            string token = _authentication.GetSessionToken();
            var products = Title.GetProducts(token,title);
            return products;
        }
        public Track[] GetProductByTuneCode(string tuneCode)
        {
            string token = _authentication.GetSessionToken();
            var products = Tunecode.GetProducts(token,tuneCode);
            return products;
        }

    }
}
