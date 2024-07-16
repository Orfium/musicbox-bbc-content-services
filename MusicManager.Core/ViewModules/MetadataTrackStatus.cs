using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class MetadataTrackStatus
    {
        public Guid TrackId { get; set; }
        public bool Deleted { get; set; }
    }
}
