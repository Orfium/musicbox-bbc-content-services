using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class TransferAtom
    {
        [JsonProperty("bms.destination")]
        public string BmsDestination { get; set; }
    }
}