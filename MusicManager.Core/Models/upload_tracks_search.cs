using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class upload_tracks_search
    {
        public Guid? id { get; set; }
        public int? session_id { get; set; }
        [Column(TypeName = "character varying")]
        public string track_name { get; set; }
        public int? size { get; set; }
        public int? status { get; set; }
        [Column(TypeName = "character varying")]
        public string s3_id { get; set; }
        public Guid? dh_track_id { get; set; }
        [Column(TypeName = "character varying")]
        public string track_type { get; set; }
        public bool? modified { get; set; }
        public bool? asset_uploaded { get; set; }
        [Column(TypeName = "character varying")]
        public string asset_upload_status { get; set; }
        public DateTime? asset_upload_begin { get; set; }
        public DateTime? asset_upload_last_check { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime? date_created { get; set; }
        public int? created_by { get; set; }
        public Guid? metadata_json { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        [Column(TypeName = "character varying")]
        public string search_string { get; set; }
        public Guid? dh_album_id { get; set; }
        public Guid? ml_album_id { get; set; }
        public bool? artwork_uploaded { get; set; }
        public bool? dh_synced { get; set; }
        public Guid? ws_id { get; set; }
        [Column(TypeName = "character varying")]
        public string performer { get; set; }
        [Column(TypeName = "character varying")]
        public string album_name { get; set; }
        [StringLength(8)]
        public string rec_type { get; set; }
        public string created_user { get; set; }
        public string edited_user { get; set; }
        [Column(TypeName = "character varying")]
        public string created_user_img { get; set; }
        [Column(TypeName = "character varying")]
        public string edited_user_img { get; set; }
        public Guid? upload_id { get; set; }
        public Guid? album_upload_id { get; set; }
        [Column(TypeName = "character varying")]
        public string artwork { get; set; }
        [Column(TypeName = "character varying")]
        public string catalogue_number { get; set; }
        public bool? archived { get; set; }
        public int? position { get; set; }
        [Column(TypeName = "character varying")]
        public string isrc { get; set; }
        [Column(TypeName = "character varying")]
        public string iswc { get; set; }
        [Column(TypeName = "character varying")]
        public string file_name { get; set; }
        public double? duration { get; set; }
        public int? disc_no { get; set; }
        public long? upload_session_id { get; set; }
        public string disply_status { get; set; }
    }
}
