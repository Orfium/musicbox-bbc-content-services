using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("playout_response", Schema = "playout")]
    public partial class playout_response
    {
        [Key]
        public long response_id { get; set; }
        public Guid build_id { get; set; }
        [Column(TypeName = "json")]
        public string response_json { get; set; }
        [Column(TypeName = "character varying")]
        public string status { get; set; }
        public long? response_time { get; set; }
        public Guid? request_id { get; set; }
    }
}
