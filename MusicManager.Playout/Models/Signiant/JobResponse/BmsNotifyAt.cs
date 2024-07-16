using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsNotifyAt
    {
        [JsonPropertyName("bms.replyTo")]
        public string BmsReplyTo { get; set; }

        [JsonPropertyName("bms.faultTo")]
        public string BmsFaultTo { get; set; }
    }
}