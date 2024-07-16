using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class MetadataWorkspace
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool deleted { get; set; }
        public int trackCount { get; set; }
    }
}
