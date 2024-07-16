using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class ActivityLog
    {
        public int activityId { get; set; }        
        public object activityDetails { get; set; }
        public string userEmail { get; set; }
        public string orgId { get; set; }
        public string orgName { get; set; }
        public string userId { get; set; }
        public string userName { get; set; }
        public string company { get; set; }
        public string division { get; set; }
        public string department { get; set; }
        public string costCode { get; set; }
        public string id { get; set; }
        public string activityName { get; set; }
        public DateTime timestamp { get; set; }
        public string ip { get; set; }
    }

    public class ServiceLog
    {
        public int id { get; set; }
        public string serviceName { get; set; }
        public DateTime timestamp { get; set; }
        public long unixtime { get; set; }
        public string status { get; set; }
        public string ip { get; set; }
        public object summary { get; set; }
        public string  refId { get; set; }
    }

    public class ServiceLogTime
    {
        public int serviceTypeId { get; set; }        
        public DateTime lastLogTime { get; set; }       
    }
}
