using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IDeliveryDestinationS3ClientRepository
    {
        Task<bool> UploadFile(string key, Stream stream);
        //Task<bool> DeleteFolder(string key, Stream stream);
        Task<bool> DeleteByKeys(List<string> keys);
    }
}
