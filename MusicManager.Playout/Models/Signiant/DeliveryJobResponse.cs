using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant
{
    public class DeliveryJobResponse
    {
        [JsonProperty("tms.transferJob")] 
        public TmsTransferJob TmsTransferJob { get; set; }
    }
}