using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Playout.Metadata
{
    public interface InterestedPartyUsage
    {
        public decimal? Share { get; set; }
        public InterestedPartyCountry Country { get; set; }
        public bool BuyOut { get; set; }
    }
}
