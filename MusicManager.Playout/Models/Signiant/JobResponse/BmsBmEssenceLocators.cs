using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmEssenceLocators
    {
        [JsonPropertyName("bms.bmEssenceLocator")]
        public List<BmsBmEssenceLocator> BmsBmEssenceLocator { get; set; }
    }
}