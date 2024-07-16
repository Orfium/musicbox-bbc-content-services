using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class workspace_org
    {
        [Key]
        public Guid org_workspace_id { get; set; }
        public Guid workspace_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        public int? ml_status { get; set; }
        public int? sync_status { get; set; }
        public bool? restricted { get; set; }
        public bool? archived { get; set; }
        [Column(TypeName = "character varying")]
        public string notes { get; set; }
        public DateTime date_created { get; set; }
        public DateTime date_last_edited { get; set; }
        public int created_by { get; set; }
        public int last_edited_by { get; set; }
        public int? index_status { get; set; }
        public int? album_sync_status { get; set; }
        public int? album_index_status { get; set; }
        public long? last_sync_api_result_id { get; set; }
        public long? last_album_sync_api_result_id { get; set; }
        public int? music_origin { get; set; }
    }
}
