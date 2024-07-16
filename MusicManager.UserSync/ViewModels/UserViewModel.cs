using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.UserSync.ViewModels
{
    public class UserViewModel
    {
        public int id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string orgId { get; set; }
        public string imageUrl { get; set; }
        public int roleid { get; set; }

    }

    public class SyncUserViewModel
    {       
        public int user_id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string org_id { get; set; }
        public string image_url { get; set; }
        public int role_id { get; set; }

    }
}
