using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class AWSS3Repository : IAWSS3Repository
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<AWSS3Repository> _logger;
        private AmazonS3Client _client;

        public AWSS3Repository(IOptions<AppSettings> appSettings, ILogger<AWSS3Repository> logger)
        {
            _appSettings = appSettings;
            _logger = logger;
            _client = new AmazonS3Client(_appSettings.Value.AWSS3.AccessKey, _appSettings.Value.AWSS3.SecretKey, Amazon.RegionEndpoint.EUWest1);
        }

        public async Task<AWSAccess> GenerateS3SessionTokenAsync()
        {
            try
            {
                AmazonSecurityTokenServiceConfig mASTconfig = new AmazonSecurityTokenServiceConfig();
                AmazonSecurityTokenServiceClient cAS3STServiceClient = new AmazonSecurityTokenServiceClient(_appSettings.Value.AWSS3.AccessKey
                                                                           , _appSettings.Value.AWSS3.SecretKey, mASTconfig);

                GetFederationTokenRequest mGetFederationTokenRequest = new GetFederationTokenRequest();
                mGetFederationTokenRequest.DurationSeconds = 86400;
                mGetFederationTokenRequest.Name = _appSettings.Value.AWSS3.TokenName;

                mGetFederationTokenRequest.Policy = $"{{\"Version\":\"2012-10-17\",\"Statement\": [{{\"Sid\":\"1\",\"Action\":[\"s3:PutObject\"],\"Effect\":\"Allow\",\"Resource\":[\"arn:aws:s3:::{_appSettings.Value.AWSS3.BucketName}/*.flac\",\"arn:aws:s3:::{_appSettings.Value.AWSS3.BucketName}/*.mp3\",\"arn:aws:s3:::{_appSettings.Value.AWSS3.BucketName}/*.ogg\",\"arn:aws:s3:::{_appSettings.Value.AWSS3.BucketName}/*.wav\",\"arn:aws:s3:::{_appSettings.Value.AWSS3.BucketName}/*.jpg\",\"arn:aws:s3:::{_appSettings.Value.AWSS3.BucketName}/*.jpeg\",\"arn:aws:s3:::{_appSettings.Value.AWSS3.BucketName}/*.png\"],\"Condition\":{{\"StringLike\": {{\"aws:Referer\":[\"{_appSettings.Value.Domain}/*\"]}}}}}}]}}";
                
                GetFederationTokenResponse mGetFederationTokenResponse = await cAS3STServiceClient.GetFederationTokenAsync(mGetFederationTokenRequest).ConfigureAwait(false);
                Credentials mCredentials = mGetFederationTokenResponse.Credentials;

                var mAWSAccess = new AWSAccess();
                mAWSAccess.AWSKey = mCredentials.AccessKeyId;
                mAWSAccess.Secretkey = mCredentials.SecretAccessKey;
                mAWSAccess.SessionToken = mCredentials.SessionToken;

                return mAWSAccess;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public string GeneratePreSignedURL(string fileName)
        {
            string urlString = "";
            try
            {
                GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest
                {
                    BucketName = _appSettings.Value.AWSS3.BucketName,
                    Key = fileName,
                    Expires = DateTime.Now.AddYears(10)
                };
                return _client.GetPreSignedURL(request1);
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "GeneratePreSignedURL > " + fileName);                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GeneratePreSignedURL > " + fileName);
            }
            return urlString;
        }

        private AmazonS3Client CreateS3Client(string serviceURL)
        {
            if (!serviceURL.Contains("https"))
                serviceURL = $"https://{serviceURL}";

            var conf = new AmazonS3Config
            {
                ServiceURL = serviceURL,
                SignatureVersion = "2"
            };

            var credentials = new BasicAWSCredentials(
                _appSettings.Value.AWSS3_ASSET_HUB.AccessKey, _appSettings.Value.AWSS3_ASSET_HUB.SecretKey);

            return new AmazonS3Client(credentials, conf);
        }

        public string GeneratePreSignedURLForMlTrack(string bucket, string key, string serviceURL, bool withoutEncode)
        {
            
            string urlString = "";
            try
            {
                var s3 = CreateS3Client(serviceURL);

                var x = s3.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = bucket,
                    Expires = DateTime.Now + TimeSpan.FromDays(1),
                    Key = key
                });

                if (withoutEncode)
                {
                    urlString = x;
                }
                else {
                    urlString = WebUtility.UrlEncode(x);
                }               
            }
            catch (AmazonS3Exception e)
            {
                _logger.LogError(e, "GeneratePreSignedURL > " + key);                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GeneratePreSignedURL > " + key);               
            }
            return urlString;
        }

        public async Task<bool> UploadObjectAsync(byte[] fileBytes, string key)
        {
            string _tempUrl = string.Empty;

            PutObjectResponse response = null;

            using (var stream = new MemoryStream(fileBytes))
            {
                var request = new PutObjectRequest
                {
                    BucketName = _appSettings.Value.AWSS3.BucketName,
                    Key = key,
                    InputStream = stream,                   
                    CannedACL = S3CannedACL.Private
                };

                response = await _client.PutObjectAsync(request).ConfigureAwait(false);
            };

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                return false;
            }           
        }

        public static byte[] ReadFully(Stream stream)
        {
            byte[] buffer = new byte[32768];
            using (MemoryStream ms = new MemoryStream())
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        return ms.ToArray();
                    ms.Write(buffer, 0, read);
                }
            }
        }

        public async Task<byte[]> UploadArtworkToS3(string keyPath)
        {
            var client = new AmazonS3Client(_appSettings.Value.AWSS3.AccessKey, _appSettings.Value.AWSS3.SecretKey, Amazon.RegionEndpoint.EUWest1);
            byte[] result = null;
            try
            {

                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = _appSettings.Value.AWSS3.BucketName,
                    Key = keyPath
                };

                using (GetObjectResponse response = await client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                {
                    result = ReadFully(responseStream);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                _logger.LogError(amazonS3Exception, "UploadArtworkToS3 > " + keyPath);                
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UploadArtworkToS3 > " + keyPath);
            }

            return result;
        }

        public async Task<byte[]> GetImageStreamById(string keyPath)
        { 
            var client = new AmazonS3Client(_appSettings.Value.AWSS3.AccessKey, _appSettings.Value.AWSS3.SecretKey, Amazon.RegionEndpoint.EUWest1);
            byte[] result = null;
            try
            {               

                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = _appSettings.Value.AWSS3.BucketName,
                    Key = keyPath
                };

                using (GetObjectResponse response = await client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                {
                    result = ReadFully(responseStream);                    
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                _logger.LogError(amazonS3Exception, "UploadArtworkToS3 > " + keyPath);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UploadArtworkToS3 > " + keyPath);
            }

            return result;
        }
    }
}
