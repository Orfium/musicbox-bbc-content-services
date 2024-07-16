using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IAWSS3Repository
    {
        Task<AWSAccess> GenerateS3SessionTokenAsync();
        Task<byte[]> GetImageStreamById(string keyPath);
        Task<bool> UploadObjectAsync(byte[] fileBytes, string key);
        string GeneratePreSignedURL(string fileName);
        string GeneratePreSignedURLForMlTrack(string bucket, string key, string serviceURL, bool withoutEncode = false);
    }
}
