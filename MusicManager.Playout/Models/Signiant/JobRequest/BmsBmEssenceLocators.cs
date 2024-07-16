using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmEssenceLocators
    {
        [JsonProperty("bms.bmEssenceLocator")]
        public List<BmsBmEssenceLocator> BmsBmEssenceLocator { get; set; }
    }
}