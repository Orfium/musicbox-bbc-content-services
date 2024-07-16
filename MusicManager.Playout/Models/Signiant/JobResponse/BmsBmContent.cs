using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmContent
    {
        [JsonPropertyName("bms.bmContentFormats")]
        public BmsBmContentFormats BmsBmContentFormats { get; set; }
    }
}