using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class ml_master_album
    {
        [Key]
        public Guid album_id { get; set; }
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        public Guid? workspace_id { get; set; }
        public Guid? library_id { get; set; }
        public bool? archived { get; set; }
        public bool? restricted { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_last_edited { get; set; }
        public long? api_result_id { get; set; }
        public bool? synced { get; set; }
        public Guid? ml_version_id { get; set; }
    }
}
