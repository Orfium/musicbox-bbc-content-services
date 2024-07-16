using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmContentFormats
    {
        [JsonProperty("bms.bmContentFormat")]
        public List<BmsBmContentFormat> BmsBmContentFormat { get; set; }
    }
}