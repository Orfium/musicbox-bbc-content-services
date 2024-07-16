using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmContent
    {
        [JsonProperty("bms.bmContentFormats")]
        public BmsBmContentFormats BmsBmContentFormats { get; set; }
    }
}