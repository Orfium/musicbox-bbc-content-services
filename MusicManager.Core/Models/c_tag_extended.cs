using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class c_tag_extended
    {
        [Key]
        public int id { get; set; }
        public int c_tag_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string name { get; set; }
        [Column(TypeName = "character varying")]
        public string description { get; set; }
        [Column(TypeName = "json")]
        public string condition { get; set; }
        public DateTime? date_created { get; set; }
        public int? created_by { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        [Column(TypeName = "character varying")]
        public string color { get; set; }
        public int? status { get; set; }
        [Column(TypeName = "character varying")]
        public string track_id { get; set; }
        [Column(TypeName = "character varying")]
        public string notes { get; set; }
    }
}
