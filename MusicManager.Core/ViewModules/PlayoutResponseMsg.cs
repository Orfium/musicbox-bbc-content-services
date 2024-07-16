using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class PlayoutResponseMsg
    {
        public ulong DeliveryTag { get; set; }
        public string Message { get; set; }
        public string epochTime { get; set; }
    }    
}
