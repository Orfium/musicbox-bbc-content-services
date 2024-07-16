using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class tag_track
    {
        [Key]
        public long id { get; set; }
        public Guid track_id { get; set; }
        public Guid tag { get; set; }
        [Required]
        [StringLength(74)]
        public string tag_with_type { get; set; }
    }
}
