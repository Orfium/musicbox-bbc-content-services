using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class org_exclude
    {
        [Key]
        public int id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string item_type { get; set; }
        public Guid ref_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string organization { get; set; }
        public DateTime date_created { get; set; }
        public int created_by { get; set; }
    }
}
