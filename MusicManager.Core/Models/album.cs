using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class album
    {
        [Key]
        public Guid album_id { get; set; }
        [Required]
        [Column(TypeName = "json")]
        public string value { get; set; }
        public DateTime date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        public Guid? library_id { get; set; }
        public Guid? workspace_id { get; set; }
    }
}
