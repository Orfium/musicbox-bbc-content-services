using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class UserModel
    {
        public string email { get; set; }
        public string password { get; set; }
        public string returnSecureToken { get; set; } = "true";
    }

    public class FireBaseUserModel
    {
        public string kind { get; set; }
        public string localId { get; set; }
        public string email { get; set; }
        public string displayName { get; set; }
        public string idToken { get; set; }
        public string registered { get; set; }
        public string refreshToken { get; set; }
        public string expiresIn { get; set; }
    }

    public class SyncUserViewModel
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string org_id { get; set; }
        public string image_url { get; set; }
        public int role_id { get; set; }

    }
}
