using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class prior_approval_work
    {
        [Key]
        public long id { get; set; }
        public DateTime date_created { get; set; }
        public int created_by { get; set; }
        public DateTime date_last_edited { get; set; }
        public int last_edited_by { get; set; }
        [Column(TypeName = "character varying")]
        public string ice_mapping_code { get; set; }
        [Column(TypeName = "character varying")]
        public string local_work_id { get; set; }
        [Column(TypeName = "character varying")]
        public string tunecode { get; set; }
        [Column(TypeName = "character varying")]
        public string iswc { get; set; }
        [Column(TypeName = "character varying")]
        public string work_title { get; set; }
        [Column(TypeName = "character varying")]
        public string composers { get; set; }
        [Column(TypeName = "character varying")]
        public string publisher { get; set; }
        [Column(TypeName = "character varying")]
        public string matched_isrc { get; set; }
        [Column(TypeName = "character varying")]
        public string matched_dh_ids { get; set; }
        [Column(TypeName = "character varying")]
        public string broadcaster { get; set; }
        [Column(TypeName = "character varying")]
        public string artist { get; set; }
        [Column(TypeName = "character varying")]
        public string writers { get; set; }
    }
}
