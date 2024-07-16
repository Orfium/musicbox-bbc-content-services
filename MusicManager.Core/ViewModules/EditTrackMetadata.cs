using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class EditTrackMetadata
    {
        public string id { get; set; }
        public Guid? dhTrackId { get; set; }
        public Guid? albumId { get; set; }
        public Guid? UploadId { get; set; }
        public string track_title { get; set; }
        public string isrc { get; set; }
        public string iswc { get; set; }
        public string prs { get; set; }
        public string artwork_url { get; set; }
        public string file_name { get; set; }
        public string position { get; set; }
        public double? numPosition { get; set; }
        public string disc_number { get; set; }       
        public string musicorigin { get; set; } 
        public string rec_label { get; set; }
        public object recLabel { get; set; }
        public string alternate_title { get; set; }       
        public string track_notes { get; set; }
        public string source_ref { get; set; }  
        public bool pre_release { get; set; }
        public float? duration { get; set; }

        public string bpm { get; set; }
        public string tempo { get; set; }
        public string pLine { get; set; }
        public List<Tag> orgTags { get; set; }
        public List<string> org_userTags { get; set; }


        public string prs_work_tunecode { get; set; }
        public string prs_work_title { get; set; }
        public string prs_work_writers { get; set; }
        public string prs_work_publishers { get; set; }

        public string bbc_track_id { get; set; }
        public List<string> org_adminTags { get; set; }
        public string org_admin_notes { get; set; }
        public List<Contributor> contributor { get; set; } 
        public List<string> genres { get; set; }       
        public List<string> styles { get; set; }
        public List<string> moods { get; set; }
        public List<string> instruments { get; set; }
        public List<string> keywords { get; set; }
        public List<string> arrangers { get; set; }
        public List<string> composers { get; set; }
        public List<string> performers { get; set; }
        public List<string> lyricist { get; set; }
        public List<string> composer_lyricists { get; set; }
        public List<string> publishers { get; set; }
        public List<string> translators { get; set; }        
        public List<string> sub_adaptor { get; set; }        
        public List<string> adaptor { get; set; }        
        public List<string> sub_arranger { get; set; }
        public List<string> sub_lyricist { get; set; }
        public List<string> original_publisher { get; set; }
        public List<string> sub_publisher { get; set; }
        public Guid? version_id { get; set; }
        public string production_library { get; set; }
        public string prs_tune_code { get; set; }
        public string version_title { get; set; }
        public string sub_origin { get; set; }
        public string valid_from_date { get; set; }
        public string valid_to_date { get; set; }
    }
}
