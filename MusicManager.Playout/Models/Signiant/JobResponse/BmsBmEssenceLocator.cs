using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmEssenceLocator
    {
        [JsonPropertyName("@xsi.type")]
        public string XsiType { get; set; }

        [JsonPropertyName("bms.file")]
        public string BmsFile { get; set; }
    }
}