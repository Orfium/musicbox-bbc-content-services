using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("playout_response_status", Schema = "playout")]
    public partial class playout_response_status
    {
        [Key]
        public long id { get; set; }
        [Column(TypeName = "character varying")]
        public string status { get; set; }
        [Column(TypeName = "character varying")]
        public string display_status { get; set; }
    }
}
