using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmContents
    {
        [JsonProperty("bms.bmContent")]
        public List<BmsBmContent> BmsBmContent { get; set; }
    }
}