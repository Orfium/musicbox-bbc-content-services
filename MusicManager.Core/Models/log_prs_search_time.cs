using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_prs_search_time", Schema = "log")]
    public partial class log_prs_search_time
    {
        [Key]
        public long id { get; set; }
        public Guid track_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string search_type { get; set; }
        [Column(TypeName = "character varying")]
        public string search_query { get; set; }
        [Column(TypeName = "time without time zone")]
        public TimeSpan time { get; set; }
        public DateTime date_created { get; set; }
    }
}
