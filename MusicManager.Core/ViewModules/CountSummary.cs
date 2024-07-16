using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class CountSummary
    {
        public long validIndexCount { get; set; }
        public long archiveIndexCount { get; set; }
        public long restrictIndexCount { get; set; }
        public long sourceDeletedIndexCount { get; set; }
        public int orgTrackCount { get; set; }
        public int masterTrackCount { get; set; }
        public int orgAlbumCount { get; set; }
        public int masterAlbumCount { get; set; }
        public long validAlbumIndexCount { get; set; }
        public long ArchivedAlbumIndexCount { get; set; }
        public long sourceDeleteAlbumIndexCount { get; set; }
        public long restrictAlbumIndexCount { get; set; }
        public long indexedCtagCount { get; set; }
        public long prsIndexedCount { get; set; }
        public long prsNotMatchedCount { get; set; }
    }
}
