using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_track_api_results", Schema = "log")]
    public partial class log_track_api_results
    {
        [Key]
        public long id { get; set; }
        public long api_call_id { get; set; }
        public Guid track_id { get; set; }
        public Guid? workspace_id { get; set; }
        public Guid? version_id { get; set; }
        public long? received { get; set; }
        public bool? deleted { get; set; }
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        public int? session_id { get; set; }
        [Column(TypeName = "date")]
        public DateTime date_created { get; set; }
        public Guid created_by { get; set; }
    }
}
