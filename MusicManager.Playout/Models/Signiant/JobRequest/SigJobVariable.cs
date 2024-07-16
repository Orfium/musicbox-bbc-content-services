using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class SigJobVariable
    {
        [JsonProperty("sig.JobVariableName")]
        public string SigJobVariableName { get; set; }

        [JsonProperty("sig.JobVariableValue")]
        public string SigJobVariableValue { get; set; }
    }
}