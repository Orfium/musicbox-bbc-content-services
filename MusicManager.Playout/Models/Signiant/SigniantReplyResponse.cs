using MusicManager.Playout.Models.Signiant.JobResponse;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant
{
    public class SigniantReplyResponse
    {
        [JsonPropertyName("transferJob")]
        public TransferJob TransferJob { get; set; }

    }
}
