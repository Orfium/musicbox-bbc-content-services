using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class workspace_pause
    {
        [Key]
        public Guid id { get; set; }
        public Guid workspace_id { get; set; }
        [Column(TypeName = "timestamp(0) without time zone")]
        public DateTime date_created { get; set; }
        public int created_by { get; set; }
        public int last_download_status { get; set; }
    }
}
