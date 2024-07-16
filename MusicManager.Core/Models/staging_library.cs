using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("staging_library", Schema = "staging")]
    public partial class staging_library
    {
        [Key]
        public Guid library_id { get; set; }
        [Column(TypeName = "character varying")]
        public string library_name { get; set; }
        public Guid workspace_id { get; set; }
        public int track_count { get; set; }
        public long? date_created { get; set; }
        public int? created_by { get; set; }
        [StringLength(5)]
        public string dh_status { get; set; }
        [StringLength(5)]
        public string ml_status { get; set; }
        [Column(TypeName = "character varying")]
        public string notes { get; set; }
        [StringLength(5)]
        public string sync_status { get; set; }
        public bool deleted { get; set; }
    }
}
