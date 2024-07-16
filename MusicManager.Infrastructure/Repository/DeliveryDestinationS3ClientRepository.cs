using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class DeliveryDestinationS3ClientRepository : IDeliveryDestinationS3ClientRepository
    {
        private AmazonS3Client _client;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<DeliveryDestinationS3ClientRepository> _logger;
        private string _defaultBucket;

        public DeliveryDestinationS3ClientRepository(
            IOptions<AppSettings> appSettings, 
            ILogger<DeliveryDestinationS3ClientRepository> logger)
        {
            _appSettings = appSettings;
            _defaultBucket = _appSettings.Value.DeliveryDestinationS3Configuration.DefaultBucket;
            _logger = logger;
            _client = new AmazonS3Client(_appSettings.Value.DeliveryDestinationS3Configuration.AccessKey, _appSettings.Value.DeliveryDestinationS3Configuration.SecretKey, Amazon.RegionEndpoint.EUWest2);
        }

        public async Task<bool> UploadFile(string key, Stream stream)
        {
            try
            {
                var objectRequest = new PutObjectRequest()
                {
                    BucketName = _defaultBucket,
                    Key = key,
                    InputStream = stream
                };

                PutObjectResponse putObjectResponse = await _client.PutObjectAsync(objectRequest);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadFile failed | key: {key} , Module: {Module}", key,  "S3 Upload");
                return false;
            }           
        }

        public async Task<bool> DeleteByKeys(List<string> keys)
        {
            try
            { 
                var objectRequest = new DeleteObjectsRequest()
                {
                    BucketName = _defaultBucket
                };

                foreach (var key in keys)
                {
                    objectRequest.AddKey(key);
                }

                DeleteObjectsResponse deleteObjectsResponse = await _client.DeleteObjectsAsync(objectRequest);
                if (deleteObjectsResponse.HttpStatusCode == System.Net.HttpStatusCode.OK) {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
