using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class SigSigniantExtensionGroup
    {
        [JsonProperty("sig.jobContextParameters")]
        public SigJobContextParameters SigJobContextParameters { get; set; }

        [JsonProperty("sig.jobVariables")]
        public List<SigJobVariable> SigJobVariables { get; set; }
    }
}