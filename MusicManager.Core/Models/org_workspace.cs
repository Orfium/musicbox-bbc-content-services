using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class org_workspace
    {
        [Key]
        public int id { get; set; }
        [Column(TypeName = "character varying")]
        public string ws_type { get; set; }
        [Column(TypeName = "character varying")]
        public string organization { get; set; }
        public Guid? dh_ws_id { get; set; }
        public DateTime? date_created { get; set; }
        public int? created_by { get; set; }
    }
}
