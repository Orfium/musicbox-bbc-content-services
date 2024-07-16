using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class ws_and_lib
    {
        public Guid? id { get; set; }
        [Column(TypeName = "character varying")]
        public string name { get; set; }
        public Guid? parent { get; set; }
        public string type { get; set; }
        [StringLength(5)]
        public string dh_status { get; set; }
        [StringLength(5)]
        public string ml_status { get; set; }
        public long? lib_count { get; set; }
        public long? track_count { get; set; }
        [Column(TypeName = "character varying")]
        public string notes { get; set; }
        [Column(TypeName = "character varying")]
        public string dh_status_display { get; set; }
        [Column(TypeName = "character varying")]
        public string ml_status_display { get; set; }
        [Column(TypeName = "character varying")]
        public string sync_status_display { get; set; }
        public DateTime? date_last_edited { get; set; }
    }
}
