using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class ManageJobRequestType
    {
        [JsonProperty("bms:ManageJobRequestType")]
        public string BmsManageJobRequestType { get; set; }
    }
}
