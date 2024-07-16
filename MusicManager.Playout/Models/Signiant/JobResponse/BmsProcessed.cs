using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsProcessed
    {
        [JsonPropertyName("@xsi.type")]
        public string XsiType { get; set; }

        [JsonPropertyName("bms.percentageProcessedCompleted")]
        public int BmsPercentageProcessedCompleted { get; set; }

        [JsonPropertyName("bms.processedBytesCount")]
        public long BmsProcessedBytesCount { get; set; }
    }
}