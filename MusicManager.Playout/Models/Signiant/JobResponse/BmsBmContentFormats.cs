using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmContentFormats
    {
        [JsonPropertyName("bms.bmContentFormat")]
        public List<BmsBmContentFormat> BmsBmContentFormat { get; set; }
    }
}