using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class SigJobContextParameters
    {
        [JsonProperty("sig.jobName")]
        public string SigJobName { get; set; }

        [JsonProperty("sig.jobGroup")]
        public string SigJobGroup { get; set; }
    }
}