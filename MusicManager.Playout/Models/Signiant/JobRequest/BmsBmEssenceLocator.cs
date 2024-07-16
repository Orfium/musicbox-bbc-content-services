using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmEssenceLocator
    {
        [JsonProperty("@xsi.type")]
        public string XsiType { get; set; }

        [JsonProperty("bms.file")]
        public string BmsFile { get; set; }
    }
}