using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_library_change", Schema = "log")]
    public partial class log_library_change
    {
        [Key]
        public long libch_id { get; set; }
        [Required]
        [StringLength(6)]
        public string action_type { get; set; }
        public Guid? library_id { get; set; }
        [Column(TypeName = "json")]
        public string old_value { get; set; }
        [Column(TypeName = "json")]
        public string new_value { get; set; }
        public DateTime date_logged { get; set; }
    }
}
