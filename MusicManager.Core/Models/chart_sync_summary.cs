using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("chart_sync_summary", Schema = "charts")]
    public partial class chart_sync_summary
    {
        [Key]
        public long id { get; set; }
        public Guid? chart_type_id { get; set; }
        [Required]
        [Column(TypeName = "char")]
        public string type { get; set; }
        [Column(TypeName = "date")]
        public DateTime check_date { get; set; }
        public int? count { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime date_last_edited { get; set; }
    }
}
