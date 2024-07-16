using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class LogElasticTrackChange
    {
        [Key]
        public long id { get; set; }
        public Guid document_id { get; set; }
        [Column(TypeName = "json")]
        public string track_org_data { get; set; }
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        public DateTime date_created { get; set; }       
        public Guid? dh_version_id { get; set; }        
        public string workspace_name { get; set; }
        public string library_name { get; set; }
        [Column(TypeName = "json")]
        public string external_identifiers { get; set; }
        public bool? deleted { get; set; }
        public string source_ref { get; set; }
        public string ext_sys_ref { get; set; }
        public string ws_type { get; set; }
        public bool? archived { get; set; }
        public bool? restricted { get; set; }
        [Column(TypeName = "json")]
        public string edit_track_metadata { get; set; }
        [Column(TypeName = "json")]
        public string edit_album_metadata { get; set; }
        public bool? pre_release { get; set; }
        public string org_id { get; set; }
        public long received { get; set; }
        [Column(TypeName = "json")]
        public string album_metadata { get; set; }
        public Guid? album_id { get; set; }
    }
}

 
