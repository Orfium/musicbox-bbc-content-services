using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("workspace")]
    public partial class workspace1
    {
        [Key]
        public Guid workspace_id { get; set; }
        [Column(TypeName = "character varying")]
        public string workspace_name { get; set; }
        [Column(TypeName = "json")]
        public string info { get; set; }
        public long? date_created { get; set; }
        public Guid? ceated_by { get; set; }
        [StringLength(5)]
        public string dh_status { get; set; }
        [StringLength(5)]
        public string ml_status { get; set; }
        public bool? restricted { get; set; }
        public long? date_last_edited { get; set; }
        public Guid? last_edited_by { get; set; }
        public Guid? wslib_id { get; set; }
        [Column(TypeName = "character varying")]
        public string notes { get; set; }
    }
}
