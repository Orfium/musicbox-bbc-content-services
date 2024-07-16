using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_track_sync_session", Schema = "log")]
    public partial class log_track_sync_session
    {
        [Key]
        public int session_id { get; set; }
        public DateTime session_start { get; set; }
        public DateTime? session_end { get; set; }
        public Guid workspace_id { get; set; }
        public int? synced_tracks_count { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan? download_time { get; set; }
        public int? download_tracks_count { get; set; }
        public bool? status { get; set; }
        [Column(TypeName = "json")]
        public string page_token { get; set; }
    }
}
