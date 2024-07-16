using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class SigSigniantExtensionGroup
    {
        [JsonPropertyName("sig.jobContextParameters")]
        public SigJobContextParameters SigJobContextParameters { get; set; }

        [JsonPropertyName("sig.jobVariables")]
        public List<SigJobVariables> SigJobVariables { get; set; }
    }
}