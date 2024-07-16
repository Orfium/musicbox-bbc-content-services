using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    [ElasticsearchType(RelationName = "MLLibraryDocument")]
    public class MLLibraryDocument
    {        
        public Guid id { get; set; }       
        public string librarty_name { get; set; }
        public Guid workspace_id { get; set; }
        public int track_count { get; set; }
        public Guid? created_by { get; set; }        
        public string dh_status { get; set; }      
        public string ml_status { get; set; }        
        public string notes { get; set; }       
        public string sync_status { get; set; }
        public DateTime? date_last_edited { get; set; }
        public Guid? last_edited_by { get; set; }
        public bool? archived { get; set; }
        public DateTime? date_created { get; set; }
        public bool? restricted { get; set; }
    }
}
