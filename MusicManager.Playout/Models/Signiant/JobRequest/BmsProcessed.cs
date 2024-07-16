using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsProcessed
    {
        [JsonProperty("@xsi.type")]
        public string XsiType { get; set; }

        [JsonProperty("bms.percentageProcessedCompleted")]
        public int BmsPercentageProcessedCompleted { get; set; }

        [JsonProperty("bms.processedBytesCount")]
        public long BmsProcessedBytesCount { get; set; }
    }
}