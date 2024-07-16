using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_ws_lib_status_change", Schema = "log")]
    public partial class log_ws_lib_status_change
    {
        [Key]
        public long id { get; set; }
        [Required]
        [Column(TypeName = "char")]
        public string record_type { get; set; }
        public Guid record_id { get; set; }
        [Required]
        [StringLength(6)]
        public string status_name { get; set; }
        [Required]
        [StringLength(5)]
        public string old_status { get; set; }
        [Required]
        [StringLength(5)]
        public string new_status { get; set; }
        public DateTime date_created { get; set; }
        public int? created_by { get; set; }
    }
}
