using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class tag
    {
        [Key]
        public Guid tag_id { get; set; }
        public Guid tag_type_id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string tag_value { get; set; }
        public DateTime date_created { get; set; }
        public Guid? created_by { get; set; }
        public Guid? created_organisation_id { get; set; }
        public int? rating { get; set; }
        public Guid? tag_code_id { get; set; }
        public DateTime? date_last_edited { get; set; }
        public Guid? last_edited_by { get; set; }
    }
}
