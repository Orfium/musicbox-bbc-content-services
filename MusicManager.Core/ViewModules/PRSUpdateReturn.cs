using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class PRSUpdateReturn
    {
        public bool? prsFound { get; set; }
        public bool prsSearchError { get; set; }
        public bool prsSessionNotFound { get; set; }
        public MLTrackDocument mLTrackDocument { get; set; }
    }
}
