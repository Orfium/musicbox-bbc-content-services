using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class record_label
    {
        [Column(TypeName = "character varying")]
        public string label { get; set; }
        public string matched_member { get; set; }
    }
}
