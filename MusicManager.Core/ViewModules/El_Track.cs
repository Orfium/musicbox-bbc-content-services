using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class El_Track
    {
        public TrackJson SourceTrack { get; set; }
        public dynamic OrganisationData { get; set; }
        //public List<tag_track> InternalTags { get; set; }
        //public List<string> ExternalTags { get; set; }
    }
}
