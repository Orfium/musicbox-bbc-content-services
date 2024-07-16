using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant
{
    public class TmsTransferJob
    {
        [JsonProperty("bms.resourceID")]
        public string BmsResourceID { get; set; }

        [JsonProperty("bms.status")]
        public string BmsStatus { get; set; }

        [JsonProperty("bms.serviceProviderJobID")]
        public string BmsServiceProviderJobID { get; set; }
    }
}