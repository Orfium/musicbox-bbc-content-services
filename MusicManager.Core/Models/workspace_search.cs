using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class workspace_search
    {
        public string id { get; set; }
        [Column(TypeName = "character varying")]
        public string workspace_name { get; set; }
        [Column(TypeName = "json")]
        public string info { get; set; }
        public int? created_by { get; set; }
        public int? dh_status { get; set; }
        public bool? restricted { get; set; }
        public int? last_edited_by { get; set; }
        public Guid? wslib_id { get; set; }
        [Column(TypeName = "character varying")]
        public string notes { get; set; }
        public int? download_status { get; set; }
        public bool? archived { get; set; }
        public DateTime? date_last_edited { get; set; }
        public DateTime? date_created { get; set; }
        public int? track_count { get; set; }
        public int? ml_track_count { get; set; }
        [Column(TypeName = "character varying")]
        public string next_page_token { get; set; }
        public DateTime? last_sync_date { get; set; }
        public int? ml_status { get; set; }
        public int? sync_status { get; set; }
        public int? index_status { get; set; }
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "character varying")]
        public string type { get; set; }
        public int? exclude { get; set; }
        public long? library_count { get; set; }
    }
}
