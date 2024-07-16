using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class SigJobVariables
    {
        [JsonPropertyName("sig.JobVariableName")]
        public string SigJobVariableName { get; set; }

        [JsonPropertyName("sig.JobVariableValue")]
        public string SigJobVariableValue { get; set; }
    }
}