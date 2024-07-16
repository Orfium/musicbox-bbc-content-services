using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class AlbumChartInfo
    {
        public string master_album_id { get; set; }
        public string dh_album_id { get; set; }
        public string dh_workspace_id { get; set; }
        public string first_date_released { get; set; }
        public string first_pos { get; set; }
        public string highest_date_released { get; set; }
        public string highest_pos { get; set; }
        public string chart_type_id { get; set; }
        public string chart_type_name { get; set; }
    }

    public partial class TrackChartInfo
    {
        public string master_track_id { get; set; }
        public string dh_track_id { get; set; }
        public string dh_workspace_id { get; set; }
        public string first_date_released { get; set; }
        public string first_pos { get; set; }
        public string highest_date_released { get; set; }
        public string highest_pos { get; set; }
        public string chart_type_id { get; set; }
        public string chart_type_name { get; set; }
    }
}
