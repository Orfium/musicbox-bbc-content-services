using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class c_tag
    {
        [Key]
        public int id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string type { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string name { get; set; }
        [Column(TypeName = "character varying")]
        public string description { get; set; }
        public DateTime? date_created { get; set; }
        [Column(TypeName = "json")]
        public string condition { get; set; }
        public int? created_by { get; set; }
        [Column(TypeName = "character varying")]
        public string colour { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        public bool? is_restricted { get; set; }
        public int? status { get; set; }
        [StringLength(6)]
        public string indicator { get; set; }
        public bool? display_indicator { get; set; }
        public int? group_id { get; set; }
    }
}
