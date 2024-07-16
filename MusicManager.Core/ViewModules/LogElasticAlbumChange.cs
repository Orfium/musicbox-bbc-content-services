using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class LogElasticAlbumChange
    {
        public Guid document_id { get; set; }
        public Guid album_id { get; set; }
        [Column(TypeName = "json")]
        public string metadata { get; set; }
        [Column(TypeName = "json")]
        public string album_org_data { get; set; }  
        public DateTime date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        public Guid? library_id { get; set; }
        public Guid? workspace_id { get; set; }
        public string workspace_name { get; set; }
        public string library_name { get; set; }
        public string library_notes { get; set; }
        public bool? restricted { get; set; }
        public bool? archived { get; set; }
        public bool? deleted { get; set; }
        public string ws_type { get; set; }
    }
}
