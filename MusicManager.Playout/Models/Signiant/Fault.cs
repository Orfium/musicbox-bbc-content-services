using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant
{
    public class Fault
    {
        [JsonProperty("bms.code")]
        public string BmsCode { get; set; }

        [JsonProperty("bms.description")]
        public string BmsDescription { get; set; }

        [JsonProperty("bms.detail")]
        public string BmsDetail { get; set; }
    }
}
