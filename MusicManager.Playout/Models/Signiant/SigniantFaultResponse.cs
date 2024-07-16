using MusicManager.Playout.Models.Signiant.JobResponse;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant
{
    public class SigniantFaultResponse
    {
        [JsonPropertyName("transferFaultNotificationType")]
        public TransferFaultNotificationType TransferFaultNotificationType { get; set; }
    }
}
