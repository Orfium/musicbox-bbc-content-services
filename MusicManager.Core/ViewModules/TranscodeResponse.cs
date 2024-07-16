using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public sealed class TranscodeResponse
    {
        public string assetUrl { get; set; }
        public string preSignedUrl { get; set; }
        public object originalTrackInfo { get; set; }

    }
}
