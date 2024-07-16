using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class SyncSummary
    {
        public int New_Tracks_Count { get; set; }
        public int Updated_Tracks_Count { get; set; }
        public long PRS_Search_Count { get; set; }
        public long PRS_Found_Count { get; set; }
        public long PRS_not_Found_Count { get; set; }
    }
}
