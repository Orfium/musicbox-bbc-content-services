using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsNotifyAt
    {
        [JsonProperty("bms.replyTo")]
        public string BmsReplyTo { get; set; }

        [JsonProperty("bms.faultTo")]
        public string BmsFaultTo { get; set; }
    }
}