using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class track
    {
        [Key]
        public Guid track_id { get; set; }
        public Guid workspace_id { get; set; }
        public Guid version_id { get; set; }
        public long received { get; set; }
        public bool deleted { get; set; }
        [Required]
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        public Guid? album_id { get; set; }
        public Guid? library_id { get; set; }
        public DateTime date_last_edited { get; set; }
        public bool? restricted { get; set; }
    }
}
