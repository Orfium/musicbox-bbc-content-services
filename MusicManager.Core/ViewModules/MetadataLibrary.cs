using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class MetadataLibrary
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool deleted { get; set; }
        public string workspaceid { get; set; }
        public int trackCount { get; set; }
    }
}
