using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class MLMasterTrack : ml_master_track 
    {
        public Guid original_track_id { get; set; }
        [Column(TypeName = "json")]
        public string c_tags { get; set; }
    }
}
