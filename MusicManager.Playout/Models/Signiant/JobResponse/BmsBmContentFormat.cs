using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmContentFormat
    {
        [JsonPropertyName("bms.bmEssenceLocators")]
        public BmsBmEssenceLocators BmsBmEssenceLocators { get; set; }

        [JsonPropertyName("bms.packageSize")]
        public long BmsPackageSize { get; set; }
    }
}