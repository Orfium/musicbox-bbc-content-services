using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class org_track_version
    {
        [Key]
        public Guid ml_version_id { get; set; }
        public Guid org_id { get; set; }
        public Guid original_track_id { get; set; }
        public Guid? original_workspace_id { get; set; }
        public Guid? new_workspace_id { get; set; }
        public long? received { get; set; }
        public bool deleted { get; set; }
        [Required]
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        public DateTime? DateCreated { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? DateLastEdited { get; set; }
        public Guid? LastEditedBy { get; set; }
    }
}
