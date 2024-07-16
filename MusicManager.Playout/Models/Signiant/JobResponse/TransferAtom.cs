using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class TransferAtom
    {
        [JsonPropertyName("bms.destination")]
        public string BmsDestination { get; set; }
    }
}