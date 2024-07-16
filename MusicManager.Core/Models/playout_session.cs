using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("playout_session", Schema = "playout")]
    public partial class playout_session
    {
        [Key]
        public int id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "date")]
        public DateTime session_date { get; set; }
        public int? track_count { get; set; }
        public int? last_status { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_last_edited { get; set; }
        public int created_by { get; set; }
        public Guid? station_id { get; set; }
        public Guid? build_id { get; set; }
        [Column(TypeName = "json")]
        public string request_json { get; set; }
        public int? last_edited_by { get; set; }
        [Column(TypeName = "character varying")]
        public string signiant_ref_id { get; set; }
        public int? publish_status { get; set; }
        public int? publish_attempts { get; set; }
        public bool s3_cleanup { get; set; }
        public DateTime? publish_start_datetime { get; set; }
        public bool notification_sent { get; set; }
    }
}
