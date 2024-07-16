using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IPreSignedUrlProvider
    {
        string GetPresignedUrl(string bucket, string assetKey);
    }
}
