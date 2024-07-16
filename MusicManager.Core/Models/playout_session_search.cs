using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class playout_session_search
    {
        public int? id { get; set; }
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "date")]
        public DateTime? session_date { get; set; }
        public Guid? station_id { get; set; }
        public int? created_by { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        public string build_id { get; set; }
        public int? last_status { get; set; }
        public long? track_count { get; set; }
        public long? publish_sucess_track_count { get; set; }
        [Column(TypeName = "character varying")]
        public string created_user_img { get; set; }
        [Column(TypeName = "character varying")]
        public string station_name { get; set; }
        public string created_user { get; set; }
        [Column(TypeName = "character varying")]
        public string signiant_ref_id { get; set; }
        public int? publish_status { get; set; }
        public DateTime? publish_start_datetime { get; set; }
        public bool? notification_sent { get; set; }
        public int? publish_attempts { get; set; }
        public bool? fully_publised { get; set; }
    }
}
