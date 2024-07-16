using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmObject
    {
        [JsonPropertyName("bms.bmContents")]
        public BmsBmContents BmsBmContents { get; set; }
    }
}