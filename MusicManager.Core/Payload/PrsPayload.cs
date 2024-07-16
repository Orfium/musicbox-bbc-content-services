using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.Payload
{
    public class PrsPayload
    {       
        public string trackId { get; set; }
        public int? ctagId { get; set; }        
    }

    public class CTagRulesPayload
    {
        public string property { get; set; }
        public string innerProperty { get; set; }
        public EnTagCondition? condition { get; set; }
        public string value { get; set; }        
    }

    public class TagCondition
    {
        public CTagRulesPayload defaultItem { get; set; }
        public CTagRulesPayload and { get; set; }
        public List<CTagRulesPayload> or { get; set; }
        public bool inlineCond { get; set; }
        public bool matched { get; set; }
    }

    public class RuleDetails
    {
        public int ruleId { get; set; }
        public string ruleName { get; set; }
        public string notes { get; set; }
    }

    public partial class ManualPRSUpdate
    {
        public string dhTrackId { get; set; }
        public string tunecode { get; set; }
        public string userId { get; set; }
        public string orgId { get; set; }
    }

    public enum  EnTagCondition: byte
    {
        Exact = 1,
        Contains = 2,
        StartWith = 3,
        EndWith = 4,
        NotContains = 5,
        ExactWord = 6,
        Boolean = 7
    }


}
