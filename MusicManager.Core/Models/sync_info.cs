using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class sync_info
    {
        [Key]
        public int id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string type { get; set; }
        public Guid? workspace_id { get; set; }
        public DateTime? last_synced_date { get; set; }
        public DateTime date_created { get; set; }
    }
}
