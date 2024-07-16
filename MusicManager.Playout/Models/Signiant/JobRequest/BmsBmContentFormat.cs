using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmContentFormat
    {
        [JsonProperty("bms.bmEssenceLocators")]
        public BmsBmEssenceLocators BmsBmEssenceLocators { get; set; }

        [JsonProperty("bms.packageSize")]
        public long BmsPackageSize { get; set; }
    }
}