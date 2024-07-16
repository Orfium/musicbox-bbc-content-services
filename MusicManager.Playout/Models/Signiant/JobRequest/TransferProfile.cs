using System.Collections.Generic;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class TransferProfile
    {
        [JsonProperty("bms.location")]
        public string BmsLocation { get; set; }

        [JsonProperty("bms.ExtensionGroup")]
        public BmsExtensionGroup BmsExtensionGroup { get; set; }
        public List<TransferAtom> transferAtom { get; set; }
    }
}