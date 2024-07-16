using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("elastic_album_change", Schema = "log")]
    public partial class elastic_album_change
    {
        [Key]
        public Guid document_id { get; set; }
        public Guid original_album_id { get; set; }
        public Guid org_workspace_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "json")]
        public string album_org_data { get; set; }
        public bool? deleted { get; set; }
        public bool? archived { get; set; }
        public DateTime date_created { get; set; }
        public bool? restricted { get; set; }
        public long? api_result_id { get; set; }
    }
}
