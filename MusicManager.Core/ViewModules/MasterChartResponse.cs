using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class MasterTrackChartResponse
    {
        public Guid chart_id { get; set; }
        public DateTime? chart_date { get; set; }
        public int result_count { get; set; }
        public List<chart_master_tracks> results { get; set; }
    }

    public partial class MasterAlbumChartResponse
    {
        public Guid chart_id { get; set; }
        public DateTime? chart_date { get; set; }
        public int result_count { get; set; }
        public List<chart_master_albums> results { get; set; }
    }
}
