using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class Album
    {
        public Guid album_id { get; set; }       
        [Column(TypeName = "json")]
        public string value { get; set; }
        public DateTime date_created { get; set; }
        public DateTime? date_last_edited { get; set; }
        public Guid? library_id { get; set; }
        public Guid? workspace_id { get; set; }
        public string workspace_name { get; set; }
        public string librarty_name { get; set; }
        public string library_notes { get; set; }
    }
}
