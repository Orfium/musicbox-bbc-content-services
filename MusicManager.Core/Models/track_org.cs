using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class track_org
    {
        public Guid id { get; set; }
        [Key]
        public Guid original_track_id { get; set; }
        [Key]
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "json")]
        public string change_log { get; set; }
        [Column(TypeName = "json")]
        public string tags { get; set; }
        public DateTime date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        [Column(TypeName = "json")]
        public string c_tags { get; set; }
        public Guid? album_id { get; set; }
        public bool? source_deleted { get; set; }
        public bool? restricted { get; set; }
        public bool? archive { get; set; }
        [Column(TypeName = "json")]
        public string org_data { get; set; }
        public int created_by { get; set; }
        public int last_edited_by { get; set; }
        public int ml_status { get; set; }
        public bool? manually_deleted { get; set; }
        public Guid org_workspace_id { get; set; }
        public long? api_result_id { get; set; }
        [Column(TypeName = "json")]
        public string prs_details { get; set; }
        [Column(TypeName = "json")]
        public string chart_info { get; set; }
        public bool? chart_artist { get; set; }
        public bool? content_alert { get; set; }
        public DateTime? content_alerted_date { get; set; }
        public int? content_alerted_user { get; set; }
        public DateTime? ca_resolved_date { get; set; }
        public int? ca_resolved_user { get; set; }
        public int? alert_type { get; set; }
        [Column(TypeName = "character varying")]
        public string alert_note { get; set; }
        public bool clearance_track { get; set; }
    }
}
