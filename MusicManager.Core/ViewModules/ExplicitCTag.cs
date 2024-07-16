using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class ExplicitCTag
    {
        public Guid trackId { get; set; }
        public string indicator { get; set; }
        public string colour { get; set; }
        public bool? result { get; set; }
    }

    public partial class ICTag
    {       
        public int ctagId { get; set; }
        public string indicator { get; set; }
        public string name { get; set; }
        public string colour { get; set; }
        public bool? result { get; set; }
    }

    public partial class IndicateCTag
    {
        public Guid trackId { get; set; }
        public List<ICTag> cTagList { get; set; }       
    }

    public partial class IndicateCTagWithRule
    {
        public ICTag cTag { get; set; }
        public List<TagCondition> conditions { get; set; }
        public RuleDetails ruleDetails { get; set; }

    }
}
