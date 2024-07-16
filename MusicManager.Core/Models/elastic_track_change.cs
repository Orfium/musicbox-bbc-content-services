using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("elastic_track_change", Schema = "log")]
    public partial class elastic_track_change
    {
        [Key]
        public long id { get; set; }
        public Guid document_id { get; set; }
        public Guid? original_track_id { get; set; }
        public Guid? dh_version_id { get; set; }
        [Required]
        [Column(TypeName = "json")]
        public string track_org_data { get; set; }
        public DateTime date_created { get; set; }
        public Guid? album_id { get; set; }
        public bool? deleted { get; set; }
        public bool? archived { get; set; }
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        public Guid org_workspace_id { get; set; }
        public bool? restricted { get; set; }
    }
}
