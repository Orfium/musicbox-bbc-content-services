using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Playout.Metadata
{
    internal interface InterestedPartyIdentity
    {

        Guid? Id { get; set; }
        Name Name { get; set; }
        Name[] AlternativeNames { get; set; }
        string Role { get; set; }
        string Society { get; set; }
        string Ipi { get; set; }
        string LabelCode { get; set; }
        string Ipn { get; set; }
        string Isni { get; set; }
        public string Custom { get; set; }
        public bool Locked { get; set; }
        public bool Verified { get; set; }
    }
}
