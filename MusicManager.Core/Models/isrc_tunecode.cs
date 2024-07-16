using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("isrc_tunecode", Schema = "staging")]
    public partial class isrc_tunecode
    {
        [Required]
        [Column(TypeName = "character varying")]
        public string tunecode { get; set; }
        [Column(TypeName = "character varying")]
        public string isrc { get; set; }
    }
}
