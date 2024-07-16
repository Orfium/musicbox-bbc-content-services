using System;
using Soundmouse.Messaging;

namespace MusicManager.Playout.Models
{
    public class PlayoutStatusMessage : Message
    {
        public Guid Id { get; set; }
        public string Status { get; set; }
    }
}
