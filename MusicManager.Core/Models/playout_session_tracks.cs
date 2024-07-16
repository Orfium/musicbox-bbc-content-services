using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("playout_session_tracks", Schema = "playout")]
    public partial class playout_session_tracks
    {
        [Key]
        public long id { get; set; }
        public int session_id { get; set; }
        public Guid track_id { get; set; }
        public int type { get; set; }
        public int status { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_last_edited { get; set; }
        public int created_by { get; set; }
        public int last_edited_by { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string title { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string isrc { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string performer { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string track_type { get; set; }
        [Column(TypeName = "character varying")]
        public string artwork_url { get; set; }
        [Column(TypeName = "character varying")]
        public string album_title { get; set; }
        [Column(TypeName = "character varying")]
        public string label { get; set; }
        [Column(TypeName = "character varying")]
        public string dh_track_id { get; set; }
        public float? duration { get; set; }
        public int? xml_status { get; set; }
        public int? asset_status { get; set; }
    }
}
