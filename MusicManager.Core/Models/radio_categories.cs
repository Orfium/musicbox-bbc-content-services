using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("radio_categories", Schema = "playout")]
    public partial class radio_categories
    {
        [Key]
        public int category_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string category_name { get; set; }
    }
}
