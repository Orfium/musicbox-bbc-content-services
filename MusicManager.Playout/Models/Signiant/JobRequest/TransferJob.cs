using System;
using Newtonsoft.Json;

namespace MusicManager.Playout.Models.Signiant.JobRequest
{
    public class TransferJob
    {
        [JsonProperty("bms.resourceID")]
        public string BmsResourceID { get; set; }

        [JsonProperty("bms.notifyAt")]
        public BmsNotifyAt BmsNotifyAt { get; set; }

        [JsonProperty("bms.status")]
        public string BmsStatus { get; set; }

        [JsonProperty("bms.serviceProviderJobID")]
        public string BmsServiceProviderJobID { get; set; }

        [JsonProperty("bms.operationName")]
        public string BmsOperationName { get; set; }

        [JsonProperty("bms.bmObjects")]
        public BmsBmObjects BmsBmObjects { get; set; }

        [JsonProperty("bms.priority")]
        public string BmsPriority { get; set; }

        [JsonProperty("bms.jobStartedTime")]
        public DateTime BmsJobStartedTime { get; set; }

        [JsonProperty("bms.jobElapsedTime")]
        public string? BmsJobElapsedTime { get; set; }

        [JsonProperty("bms.jobCompletedTime")]
        public string? BmsJobCompletedTime { get; set; }

        [JsonProperty("bms.processed")]
        public BmsProcessed BmsProcessed { get; set; }

        [JsonProperty("profiles")]
        public Profiles Profiles { get; set; }
    }
}