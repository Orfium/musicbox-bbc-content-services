using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class upload_session_search
    {
        public long? id { get; set; }
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "date")]
        public DateTime? log_date { get; set; }
        public int? status { get; set; }
        public int? created_by { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        public string created_user { get; set; }
        [Column(TypeName = "character varying")]
        public string created_user_img { get; set; }
        public long? track_count { get; set; }
    }
}
