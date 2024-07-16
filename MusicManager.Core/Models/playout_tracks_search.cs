using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("playout_tracks_search", Schema = "playout")]
    public partial class playout_tracks_search
    {
        [Column(TypeName = "character varying")]
        public string title { get; set; }
        public long? id { get; set; }
        public Guid? track_id { get; set; }
        [Column(TypeName = "character varying")]
        public string track_type { get; set; }
        [Column(TypeName = "character varying")]
        public string performer { get; set; }
        [Column(TypeName = "character varying")]
        public string isrc { get; set; }
        public int? status { get; set; }
        public int? created_by { get; set; }
        public DateTime? date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? session_id { get; set; }
        [Column(TypeName = "character varying")]
        public string artwork_url { get; set; }
    }
}
