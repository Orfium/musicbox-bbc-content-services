using System;
using System.Text.Json.Serialization;

namespace MusicManager.Playout.Models.Signiant.JobResponse
{
    public class TransferJob
    {
        [JsonPropertyName("bms.resourceID")]
        public string BmsResourceID { get; set; }

        [JsonPropertyName("bms.notifyAt")]
        public BmsNotifyAt BmsNotifyAt { get; set; }

        [JsonPropertyName("bms.status")]
        public string BmsStatus { get; set; }

        [JsonPropertyName("bms.serviceProviderJobID")]
        public string BmsServiceProviderJobID { get; set; }

        [JsonPropertyName("bms.operationName")]
        public string BmsOperationName { get; set; }

        [JsonPropertyName("bms.bmObjects")]
        public BmsBmObjects BmsBmObjects { get; set; }

        [JsonPropertyName("bms.priority")]
        public string BmsPriority { get; set; }

        [JsonPropertyName("bms.jobStartedTime")]
        public DateTime BmsJobStartedTime { get; set; }

        [JsonPropertyName("bms.jobElapsedTime")]
        public string? BmsJobElapsedTime { get; set; }

        [JsonPropertyName("bms.jobCompletedTime")]
        public string? BmsJobCompletedTime { get; set; }

        [JsonPropertyName("bms.processed")]
        public BmsProcessed BmsProcessed { get; set; }

        [JsonPropertyName("profiles")]
        public Profiles Profiles { get; set; }
    }
}