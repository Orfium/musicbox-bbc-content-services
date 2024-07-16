using MusicManager.Core.Models;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class TrackJson
    {
        public string id { get; set; }
        public int arid { get; set; }
        public List<Asset> assets { get; set; }
        public Source source { get; set; }
        public TrackData trackData { get; set; }
        public string audioAsset { get; set; }
        public List<string> territories { get; set; }
        public Guid workspaceId { get; set; }
        public List<tag_track> InternalTags { get; set; }
        
    }    
    
}
