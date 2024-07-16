using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.PrsSearch.PrsAuth
{
    public interface IAuthentication
    {      
        string GetSessionToken(bool checkCache = true);
    }
}
