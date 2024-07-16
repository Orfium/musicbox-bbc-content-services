using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("log_album_api_calls", Schema = "log")]
    public partial class log_album_api_calls
    {
        [Key]
        public long id { get; set; }
        public DateTime date_created { get; set; }
        public Guid? ws_id { get; set; }
        public int page_size { get; set; }
        [Column(TypeName = "character varying")]
        public string page_token { get; set; }
        [Column(TypeName = "character varying")]
        public string library_ids { get; set; }
        public int? response_code { get; set; }
        [Column(TypeName = "character varying")]
        public string next_page_token { get; set; }
        public int album_count { get; set; }
        public int? session_id { get; set; }
    }
}
