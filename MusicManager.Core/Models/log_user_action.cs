using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_user_action", Schema = "log")]
    public partial class log_user_action
    {
        [Key]
        public long id { get; set; }
        public int action_id { get; set; }
        public int? user_id { get; set; }
        public DateTime date_created { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Column(TypeName = "character varying")]
        public string old_value { get; set; }
        [Column(TypeName = "character varying")]
        public string new_value { get; set; }
        [Required]
        [StringLength(5)]
        public string data_type { get; set; }
        public Guid? ref_id { get; set; }
        [Column(TypeName = "character varying")]
        public string data_value { get; set; }
        public int status { get; set; }
        [Column(TypeName = "character varying")]
        public string exception { get; set; }
    }
}
