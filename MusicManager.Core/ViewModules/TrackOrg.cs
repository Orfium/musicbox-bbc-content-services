using MusicManager.Core.Models;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class TrackOrg : track_org
    {
        public ClearanceCTags c_tags { get; set; }
        public List<Tag> org_data { get; set; }
        public List<TrackChangeLog> change_log { get; set; }
        public dynamic prs_details { get; set; }
        public TrackChartInfo chart_info { get; set; }
    }

    public partial class AlbumOrg : album_org
    {
        public List<CTagOrg> c_tags { get; set; }
        public List<Tag> org_data { get; set; }
        public AlbumChartInfo chart_info { get; set; }
        public List<TrackChangeLog> change_log { get; set; }
    }
}
