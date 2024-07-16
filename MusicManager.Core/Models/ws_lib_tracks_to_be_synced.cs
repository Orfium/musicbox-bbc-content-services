using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class ws_lib_tracks_to_be_synced
    {
        [Key]
        public long id { get; set; }
        [Required]
        [StringLength(3)]
        public string type { get; set; }
        public Guid ref_id { get; set; }
        [Required]
        [StringLength(8)]
        public string status { get; set; }
        [Column(TypeName = "date")]
        public DateTime? available_from { get; set; }
        public DateTime date_created { get; set; }
        public int created_by { get; set; }
        [StringLength(5)]
        public string elastic_status { get; set; }
        public bool? counts_updated { get; set; }
        public bool? album_indexed { get; set; }
        public DateTime? date_last_edited { get; set; }
        public Guid? reindex_ref { get; set; }
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
    }
}
