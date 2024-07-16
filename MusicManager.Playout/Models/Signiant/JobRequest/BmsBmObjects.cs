using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmObjects
    {
        [JsonProperty("bms.bmObject")]
        public List<BmsBmObject> BmsBmObject { get; set; }
    }
}