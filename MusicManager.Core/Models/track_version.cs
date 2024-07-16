using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class track_version
    {
        [Key]
        public Guid track_id { get; set; }
        public Guid? album_id { get; set; }
        public Guid? library_id { get; set; }
        public Guid? workspace_id { get; set; }
        public Guid? version_id { get; set; }
        public long? arid { get; set; }
        [Column(TypeName = "json")]
        public string value { get; set; }
        [Column(TypeName = "character varying")]
        public string title { get; set; }
        [Column(TypeName = "character varying")]
        public string duration { get; set; }
        [Column(TypeName = "character varying")]
        public string product_name { get; set; }
        [Column(TypeName = "character varying")]
        public string product_artist { get; set; }
        [Column(TypeName = "character varying")]
        public string composers { get; set; }
        [Column(TypeName = "character varying")]
        public string performers { get; set; }
        [Column(TypeName = "character varying")]
        public string publishers { get; set; }
        [Column(TypeName = "character varying")]
        public string record_labels { get; set; }
        [Column(TypeName = "character varying")]
        public string catalogue_number { get; set; }
        [Column(TypeName = "character varying")]
        public string tunecode { get; set; }
        [Column(TypeName = "character varying")]
        public string isrc { get; set; }
        [Column(TypeName = "character varying")]
        public string iswc { get; set; }
        [Column(TypeName = "character varying")]
        public string gema { get; set; }
        [Column(TypeName = "character varying")]
        public string music_origin { get; set; }
        public int? release_year { get; set; }
    }
}
