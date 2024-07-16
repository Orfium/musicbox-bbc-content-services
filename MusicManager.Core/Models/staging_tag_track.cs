using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("staging_tag_track", Schema = "staging")]
    public partial class staging_tag_track
    {
        [Key]
        public long id { get; set; }
        public Guid track_id { get; set; }
        [Column(TypeName = "json")]
        public string tags { get; set; }
    }
}
