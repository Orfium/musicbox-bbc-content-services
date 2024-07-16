using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class BmsBmObjects
    {
        [JsonPropertyName("bms.bmObject")]
        public List<BmsBmObject> BmsBmObject { get; set; }
    }
}