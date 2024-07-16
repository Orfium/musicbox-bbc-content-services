using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_elastic_track_changes", Schema = "log")]
    public partial class log_elastic_track_changes
    {
        [Key]
        public long id { get; set; }
        public Guid track_id { get; set; }
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        public DateTime date_created { get; set; }
        public bool processed { get; set; }
        public Guid version_id { get; set; }
        public long received { get; set; }
        public bool deleted { get; set; }
        public bool restricted { get; set; }
    }
}
