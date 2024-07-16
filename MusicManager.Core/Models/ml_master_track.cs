using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class ml_master_track
    {
        [Key]
        public Guid track_id { get; set; }
        public Guid workspace_id { get; set; }
        public Guid? library_id { get; set; }
        public Guid dh_version_id { get; set; }
        public long received { get; set; }
        public bool deleted { get; set; }
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        public Guid? album_id { get; set; }
        public DateTime date_last_edited { get; set; }
        public bool? restricted { get; set; }
        [StringLength(5)]
        public string dh_status { get; set; }
        [Column(TypeName = "json")]
        public string external_identifiers { get; set; }
        [Column(TypeName = "character varying")]
        public string source_ref { get; set; }
        [Column(TypeName = "character varying")]
        public string ext_sys_ref { get; set; }
        [Column(TypeName = "json")]
        public string edit_track_metadata { get; set; }
        [Column(TypeName = "json")]
        public string edit_album_metadata { get; set; }
        public bool? pre_release { get; set; }
        public long? api_result_id { get; set; }
        public bool? synced { get; set; }
        public Guid? ml_version_id { get; set; }
        public long? dh_received { get; set; }
    }
}
