using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("playout_tracks_search")]
    public partial class playout_tracks_search1
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
        [Column(TypeName = "character varying")]
        public string album_title { get; set; }
        [Column(TypeName = "character varying")]
        public string label { get; set; }
        [Column(TypeName = "character varying")]
        public string dh_track_id { get; set; }
        public int? session_status { get; set; }
        public int? xml_status { get; set; }
        public int? asset_status { get; set; }
    }
}
