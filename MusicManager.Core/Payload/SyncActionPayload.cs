using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.Payload
{
    public partial class SyncActionPayload
    {
        public string action { get; set; }
        public string userId { get; set; }
        public List<string> ids { get; set; }
        public string type { get; set; }
        public string orgid { get; set; }
        public MusicOrgPayload music_origin { get; set; }
        public Permissions permissions { get; set; }
    }

    public partial class Permissions
    {
        public bool _smContentAdmin { get; set; }
    }

    public partial class PauseActionPayload
    {
        public string workspace_id { get; set; }
        public string userId { get; set; }
        public string action { get; set; }
    }

    public partial class MusicOrgPayload
    {
        public string music_origin { get; set; }
    }
}
