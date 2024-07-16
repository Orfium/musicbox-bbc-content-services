using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsExtensionGroup
    {
        [JsonProperty("sig.signiantExtensionGroup")]
        public List<SigSigniantExtensionGroup> SigSigniantExtensionGroup { get; set; }
    }
}