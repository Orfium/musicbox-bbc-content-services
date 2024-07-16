using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class UserAction
    {
        public Guid id { get; set; }
        public int action_id { get; set; }
        public int? user_id { get; set; }
        public DateTime date_created { get; set; }
        public string org_id { get; set; }       
        public dynamic old_value { get; set; }
        public dynamic new_value { get; set; }        
        public string data_type { get; set; }
        public Guid? ref_id { get; set; }       
        public string data_value { get; set; }
        public int status { get; set; }       
        public dynamic exception { get; set; }
    }
}
