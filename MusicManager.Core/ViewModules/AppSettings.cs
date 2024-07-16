using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class AppSettings
    {
        public string AppVersion { get; set; }
        public string AppEnvironment { get; set; }
        public string MasterWSId { get; set; }
        public int ExplicitCtagId { get; set; }
        public int PPLCtagId { get; set; }
        public int SyncServiceId { get; set; }
        public int ServiceId { get; set; }
        public string NpgConnection { get; set; }
        public bool IsLocal { get; set; }
        public string Domain { get; set; }
        public ApiSettings MetadataApiSettings { get; set; }
        public ApiSettings SMSearchApiSettings { get; set; }
        public ApiSettings MusicApiSettings { get; set; }
        public ApiSettings SMCoreApiSettings { get; set; }
        public AWSS3Settings AWSS3 { get; set; }
        public AWSS3Settings AWSS3_ASSET_HUB { get; set; }
        public Elasticsearch Elasticsearch { get; set; }
        public RabbitMQConfiguration RabbitMQConfiguration { get; set; }
        public PRSSettings PRSSettings { get; set; }
        public ChartApiSettings ChartApiSettings { get; set; }
        public ServiceScheduleTime ServiceScheduleTimes { get; set; }
        public SigniantConfigs SigniantConfigs { get; set; }
        public S3Settings DeliveryDestinationS3Configuration { get; set; }
    }

    public partial class RabbitMQConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string VirtualHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string RequestQueue { get; set; }
        public string ResponseQueue { get; set; }
        public string DeliveryResponseQueue { get; set; }
    }

    public partial class ApiSettings
    {
        public string API_Endpoint { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }        
    }

    public partial class PRSSettings
    {       
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public partial class AWSS3Settings
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
        public string FolderName { get; set; }
        public string BucketUrl { get; set; }
        public string TokenKey { get; set; }
        public string TokenName { get; set; }
        public string Reagion { get; set; }
        public string ServiceUrl { get; set; }
    }

    public partial class S3Settings
    {
        public string ServiceUrl { get; set; }
        public string DefaultBucket { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }       
    }

    public partial class Elasticsearch
    {
        public string url { get; set; }
        public string track_index { get; set; }
        public string album_index { get; set; }
        public string service_log_index { get; set; }
    }

    public partial class ChartApiSettings
    {
        public string MasterTracks { get; set; }
        public string MasterAlbums { get; set; }      
    }

    public partial class ServiceScheduleTime
    {
        public int TakeDownStartHour { get; set; }
        public int TakeDownEndhour { get; set; }
        public int PRSSearchStartHour { get; set; }
        public int PRSSearchEndtHour { get; set; }
    }

    public partial class SigniantConfigs
    {
        public string FlightStorageConfigId { get; set; }
        public string TargetAgent { get; set; }
        public string SimpleFileLocatorPath { get; set; }
        public string TransferTemplate { get; set; }
        public string JobGroup { get; set; }
        public string ServiceBaseUrl { get; set; }
        public string ReplyBaseUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DeliveryDestinationBucket { get; set; }
    }
}
