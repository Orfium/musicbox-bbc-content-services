using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant
{
    public class FaultJobResponse
    {
        [JsonProperty("fault")]
        public Fault Fault { get; set; }
    }
}
