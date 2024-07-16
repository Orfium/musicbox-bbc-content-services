using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class library_search_ml_admin
    {
        public string id { get; set; }
        public Guid? olbid { get; set; }
        [Column(TypeName = "character varying")]
        public string library_name { get; set; }
        public Guid? workspace_id { get; set; }
        public int? track_count { get; set; }
        public int? ml_track_count { get; set; }
        public int? created_by { get; set; }
        public int? dh_status { get; set; }
        [Column(TypeName = "character varying")]
        public string notes { get; set; }
        public int? download_status { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        public bool? archived { get; set; }
        public DateTime? date_created { get; set; }
        [Column(TypeName = "character varying")]
        public string workspace_name { get; set; }
        public int? ml_status { get; set; }
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
    }
}
