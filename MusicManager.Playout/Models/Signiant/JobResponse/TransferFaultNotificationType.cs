using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class TransferFaultNotificationType
    {
        [JsonPropertyName("transferJob")]
        public TransferJob TransferJob { get; set; }

        [JsonPropertyName("fault")]
        public Fault Fault { get; set; }
    }
}