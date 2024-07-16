using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsExtensionGroup
    {
        [JsonPropertyName("sig.signiantExtensionGroup")]
        public List<SigSigniantExtensionGroup> SigSigniantExtensionGroup { get; set; }
    }
}