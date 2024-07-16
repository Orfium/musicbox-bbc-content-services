using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules

{
    public partial class MLPRS
    {
        public enPrsSearchType searchType { get; set; }
        public PRSWork work { get; set; }
        public Track recording { get; set; }
        public DateTime? dateTime  { get; set; }
    }

    public class PRSWork
    {
        public string[] Iswc { get; set; }
        public string[] LibraryCatalogueNumbers { get; set; }
        public MLInterestedParties[] Publishers { get; set; }
        public string Title { get; set; }
        public string Tunecode { get; set; }
        public string Type { get; set; }
        public MLInterestedParties[] Writers { get; set; }
    }

    public partial class MLInterestedParties : InterestedParty
    {
        public MLInterestedParties(string fullName, string role, string performingRightAffiliationField)
        {
            FullName = fullName;
            Role = role;
            PerformingRightAffiliationField = performingRightAffiliationField;
        }

        public string PerformingRightAffiliationField { get; set; }
    }
}

