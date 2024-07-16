using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class SigJobContextParameters
    {
        [JsonPropertyName("sig.jobName")]
        public string SigJobName { get; set; }

        [JsonPropertyName("sig.jobGroup")]
        public string SigJobGroup { get; set; }
    }
}