using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class upload_album
    {
        [Key]
        public Guid id { get; set; }
        public int session_id { get; set; }
        public Guid? dh_album_id { get; set; }
        public bool? modified { get; set; }
        public bool? artwork_uploaded { get; set; }
        [Column(TypeName = "character varying")]
        public string artist { get; set; }
        [Column(TypeName = "character varying")]
        public string album_name { get; set; }
        [Column(TypeName = "character varying")]
        public string release_date { get; set; }
        [Column(TypeName = "json")]
        public string metadata_json { get; set; }
        public DateTime date_created { get; set; }
        public int created_by { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        [Column(TypeName = "character varying")]
        public string catalogue_number { get; set; }
        [Column(TypeName = "character varying")]
        public string artwork { get; set; }
        [StringLength(8)]
        public string rec_type { get; set; }
        public Guid? copy_source_album_id { get; set; }
        public Guid? copy_source_ws_id { get; set; }
        public Guid? upload_id { get; set; }
    }
}
