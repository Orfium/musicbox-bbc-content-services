using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class tag_type
    {
        [Key]
        public Guid tag_type_id { get; set; }
        [Required]
        [Column("tag_type", TypeName = "character varying")]
        public string tag_type1 { get; set; }
    }
}
