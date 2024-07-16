using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_workspace_change", Schema = "log")]
    public partial class log_workspace_change
    {
        [Key]
        public long wsch_id { get; set; }
        [StringLength(6)]
        public string action_type { get; set; }
        public Guid workspace_id { get; set; }
        [Column(TypeName = "json")]
        public string old_value { get; set; }
        [Column(TypeName = "json")]
        public string new_value { get; set; }
        public DateTime? date_logged { get; set; }
    }
}
