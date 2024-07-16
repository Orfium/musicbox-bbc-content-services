using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class org_user
    {
        [Key]
        public int user_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string org_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string first_name { get; set; }
        [Column(TypeName = "character varying")]
        public string last_name { get; set; }
        [Column(TypeName = "character varying")]
        public string email { get; set; }
        public int? role_id { get; set; }
        public DateTime? date_last_edited { get; set; }
        [Column(TypeName = "character varying")]
        public string image_url { get; set; }
    }
}
