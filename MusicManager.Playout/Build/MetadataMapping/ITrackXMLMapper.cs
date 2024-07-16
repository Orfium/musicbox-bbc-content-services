using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Playout.Build.MetadataMapping
{
    public interface ITrackXMLMapper
    {
        public EXPORT MapTrackToFile(Track trackToMap, string xmlType, List<string> prsPublishers);
    }
}
