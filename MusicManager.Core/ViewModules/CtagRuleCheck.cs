using MusicManager.Core.Payload;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class CtagRuleCheck
    {
        public bool? result { get; set; }
        public List<TagCondition> conditions { get; set; }
        public RuleDetails ruleDetails { get; set; }
    }
}
