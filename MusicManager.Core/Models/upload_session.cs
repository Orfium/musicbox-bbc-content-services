using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class upload_session
    {
        [Key]
        public long id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "date")]
        public DateTime log_date { get; set; }
        public int? track_count { get; set; }
        public int status { get; set; }
        public DateTime date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        [Column(TypeName = "character varying")]
        public string session_name { get; set; }
        public int? created_by { get; set; }
    }
}
