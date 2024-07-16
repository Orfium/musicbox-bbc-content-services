using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class c_tag_index_status
    {
        [Key]
        [Column(TypeName = "character varying")]
        public string type { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime updated_on { get; set; }
        public int? updated_by { get; set; }
        public Guid update_idetifier { get; set; }
    }
}
