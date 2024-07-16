using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_sync_time", Schema = "log")]
    public partial class log_sync_time
    {
        [Key]
        public long id { get; set; }
        public Guid workspace_id { get; set; }
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        public DateTime? track_download_start_time { get; set; }
        public DateTime? track_download_end_time { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan? track_download_time { get; set; }
        public DateTime? album_download_start_time { get; set; }
        public DateTime? album_download_end_time { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan? album_download_time { get; set; }
        public DateTime? sync_start_time { get; set; }
        public DateTime? sync_end_time { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan? sync_time { get; set; }
        public DateTime? track_index_start_time { get; set; }
        public DateTime? track_index_end_time { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan? track_index_time { get; set; }
        public DateTime? album_index_start_time { get; set; }
        public DateTime? album_index_end_time { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan? album_index_time { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan? total_time { get; set; }
        public int? status { get; set; }
        public int? download_tracks_count { get; set; }
        public int? download_albums_count { get; set; }
        public int? sync_tracks_count { get; set; }
        public int? sync_albums_count { get; set; }
        public int? index_tracks_count { get; set; }
        public int? index_albums_count { get; set; }
        public int? service_id { get; set; }
        public DateTime? completed_time { get; set; }
    }
}
