using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.PrsModels
{
    public partial class track_prs_master
    {
        [Key]
        public Guid track_id { get; set; }
        [Column(TypeName = "character varying")]
        public string dh_prs_id { get; set; }
        [Column(TypeName = "character varying")]
        public string dh_isrc { get; set; }
        [StringLength(5)]
        public string update_source { get; set; }
        [Column(TypeName = "character varying")]
        public string prs_tune_code { get; set; }
        [Column(TypeName = "json")]
        public string prs_work_details { get; set; }
        [Column(TypeName = "json")]
        public string prs_search_details { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime date_created { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime date_last_edited { get; set; }
    }
}
