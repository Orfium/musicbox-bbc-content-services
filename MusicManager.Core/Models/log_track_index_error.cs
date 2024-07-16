using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_track_index_error", Schema = "log")]
    public partial class log_track_index_error
    {
        [Key]
        public int id { get; set; }
        public Guid? doc_id { get; set; }
        [Column(TypeName = "character varying")]
        public string error { get; set; }
        [Column(TypeName = "character varying")]
        public string reson { get; set; }
    }
}
