using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("staging_workspace", Schema = "staging")]
    public partial class staging_workspace
    {
        [Key]
        public Guid workspace_id { get; set; }
        [Column(TypeName = "character varying")]
        public string workspace_name { get; set; }
        public int? track_count { get; set; }
        public bool deleted { get; set; }
        [Column(TypeName = "character varying")]
        public string date_created { get; set; }
        [Column(TypeName = "character varying")]
        public string date_content_modified { get; set; }
    }
}
