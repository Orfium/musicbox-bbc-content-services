using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class tag_code
    {
        [Key]
        public int tag_code_id { get; set; }
        [Column("tag_code", TypeName = "character varying")]
        public string tag_code1 { get; set; }
        [StringLength(4)]
        public string code { get; set; }
    }
}
