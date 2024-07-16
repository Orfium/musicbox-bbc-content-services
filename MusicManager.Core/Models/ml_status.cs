using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class ml_status
    {
        [StringLength(5)]
        public string status_code { get; set; }
        [Column(TypeName = "character varying")]
        public string status { get; set; }
    }
}
