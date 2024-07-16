using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class member_label
    {
        [Key]
        public int id { get; set; }
        [Column(TypeName = "character varying")]
        public string member { get; set; }
        [Column(TypeName = "character varying")]
        public string label { get; set; }
        [Column(TypeName = "character varying")]
        public string mlc { get; set; }
        [StringLength(6)]
        public string source { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? created_by { get; set; }
        public int? last_edited_by { get; set; }
    }
}
