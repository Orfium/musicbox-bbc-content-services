using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class TransferProfile
    {
        [JsonPropertyName("bms.location")]
        public string BmsLocation { get; set; }

        [JsonPropertyName("bms.ExtensionGroup")]
        public BmsExtensionGroup BmsExtensionGroup { get; set; }
        public List<TransferAtom> transferAtom { get; set; }
    }
}