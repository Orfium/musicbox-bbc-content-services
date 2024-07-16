using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_ws_lib_change", Schema = "log")]
    public partial class log_ws_lib_change
    {
        [Key]
        public long ws_lib_change_id { get; set; }
        [Required]
        [Column(TypeName = "char")]
        public string record_type { get; set; }
        [Required]
        [Column(TypeName = "char")]
        public string action { get; set; }
        public Guid key { get; set; }
        [Column(TypeName = "json")]
        public string value { get; set; }
        [Column(TypeName = "date")]
        public DateTime date_loged { get; set; }
        public DateTime time_loged { get; set; }
    }
}
