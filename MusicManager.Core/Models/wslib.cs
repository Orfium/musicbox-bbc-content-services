using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MusicManager.Core.Models
{
    public partial class wslib
    {
        public Guid? id { get; set; }
        [Column(TypeName = "character varying")]
        public string name { get; set; }
        [Column(TypeName = "char")]
        public string type { get; set; }
        [Column(TypeName = "json")]
        public string info { get; set; }
        [StringLength(5)]
        public string dh_status { get; set; }
        [StringLength(5)]
        public string sync_status { get; set; }
        [StringLength(5)]
        public string ml_status { get; set; }
        public long? date_created { get; set; }
        public Guid? created_by { get; set; }
        public Guid? ml_workspace_id { get; set; }
    }
}
