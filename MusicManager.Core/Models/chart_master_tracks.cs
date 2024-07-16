using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("chart_master_tracks", Schema = "charts")]
    public partial class chart_master_tracks
    {
        [Key]
        public Guid master_id { get; set; }
        [Column(TypeName = "character varying")]
        public string title { get; set; }
        [Column(TypeName = "character varying")]
        public string artist { get; set; }
        [Column(TypeName = "character varying")]
        public string external_id { get; set; }
        [Column(TypeName = "date")]
        public DateTime? first_date_released { get; set; }
        [Column(TypeName = "date")]
        public DateTime? highest_date_released { get; set; }
        public int? first_pos { get; set; }
        public int? highest_pos { get; set; }
        public Guid chart_type_id { get; set; }
        [Column(TypeName = "character varying")]
        public string chart_type_name { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime date_created { get; set; }
        [Column(TypeName = "timestamp with time zone")]
        public DateTime date_last_edited { get; set; }
    }
}
