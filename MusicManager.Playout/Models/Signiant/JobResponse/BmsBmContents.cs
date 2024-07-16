using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmContents
    {
        [JsonPropertyName("bms.bmContent")]
        public List<BmsBmContent> BmsBmContent { get; set; }
    }
}