using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    [Table("radio_stations", Schema = "playout")]
    public partial class radio_stations
    {
        [Key]
        public Guid id { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string sys { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string station { get; set; }
        [Required]
        [Column(TypeName = "character varying")]
        public string delivery_location { get; set; }
        public int created_by { get; set; }
        public DateTime date_created { get; set; }
        public int? order { get; set; }
        public int? category_id { get; set; }
        [Column(TypeName = "character varying")]
        public string delivery_location_classical { get; set; }
    }
}
