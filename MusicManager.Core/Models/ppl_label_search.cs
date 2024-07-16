using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class ppl_label_search
    {
        public int? id { get; set; }
        [Column(TypeName = "character varying")]
        public string member { get; set; }
        [Column(TypeName = "character varying")]
        public string label { get; set; }
        [Column(TypeName = "character varying")]
        public string mlc { get; set; }
        public DateTime? date_created { get; set; }
        [StringLength(6)]
        public string source { get; set; }
    }
}
