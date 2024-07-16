using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class BmsBmObject
    {
        [JsonProperty("bms.bmContents")]
        public BmsBmContents BmsBmContents { get; set; }
    }
}