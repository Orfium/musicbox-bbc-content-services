using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.ServiceLogics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicManager.Logics.Logics
{
    public class PreSignedUrlProvider : IPreSignedUrlProvider
    {
        private readonly IOptions<AppSettings> _appSettings;

        public PreSignedUrlProvider(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }
        public string GetPresignedUrl(string bucket, string assetKey)
        {
            if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(assetKey))
                return string.Empty;

            using (var s3 = new AmazonS3Client(_appSettings.Value.AWSS3.AccessKey, _appSettings.Value.AWSS3.SecretKey, Amazon.RegionEndpoint.EUWest1))
            {
                var request = new GetPreSignedUrlRequest()
                {
                    BucketName = bucket,
                    Verb = HttpVerb.GET,
                    Expires = DateTime.Now.AddDays(1),
                    Protocol = Protocol.HTTPS,
                    Key = assetKey,
                };

                return s3.GetPreSignedURL(request);
            }
        }

        private static readonly Regex _assetHubUrl = new Regex("http(s)?\\:\\/\\/(?<bucket>[^\\.]+)\\.assets\\.soundmouse\\.com\\/(?<key>.+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
     

        public static (string bucket, string key) ExtractBucketAndKeyFromAssetHubUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return (string.Empty, string.Empty);

            if (!url.Contains("assets.soundmouse.com"))
                return (string.Empty, url);

            var match = _assetHubUrl.Match(url);

            if (!match.Success)
                return (string.Empty, string.Empty);

            return (match.Groups["bucket"].Value, match.Groups["key"].Value);
        }
    }
}
