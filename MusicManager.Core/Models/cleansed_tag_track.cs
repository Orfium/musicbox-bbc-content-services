using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class cleansed_tag_track
    {
        [Key]
        public long id { get; set; }
        public Guid track_id { get; set; }
        public Guid? organization_id { get; set; }
        public Guid? tag_type_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string tag_type_name { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string tag_name { get; set; }
        public int rating { get; set; }
        public bool? is_cleansed { get; set; }
        [Column(TypeName = "character varying")]
        public string cleansed_algorithm_id { get; set; }
        public Guid? created_by { get; set; }
        public DateTime date_created { get; set; }
        public Guid? last_edited_by { get; set; }
        public DateTime? date_last_edited { get; set; }
    }
}
