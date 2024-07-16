using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class DHTrackEdit
    {
        public EditTrackMetadata trackMetadata { get; set; }
        public EditAlbumMetadata albumMetadata { get; set; }
        public DHTrack dHTrack { get; set; }
        public DHAlbum dHAlbum { get; set; }
        public string wsType { get; set; }
        public Guid wsId { get; set; }
    }
}
