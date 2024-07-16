using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class MA_Track
    {
        public string id { get; set; }
        public string uniqueId { get; set; }
        public Guid? albumId { get; set; }
        public string title { get; set; }
        public string musicOrigin { get; set; }
        public List<MA_Identifier> identifiers { get; set; }
        public MA_Territories territories { get; set; }
        public List<MA_InterestedParty> interestedParties { get; set; }
        public MA_Miscellaneous miscellaneous { get; set; }

    }

    public class MA_Track_initial
    {
        public string uniqueId { get; set; }      
        public string title { get; set; }
        public string musicOrigin { get; set; }      
        public MA_Territories territories { get; set; }        
        public MA_Miscellaneous miscellaneous { get; set; }
    }

    public class MA_Identifier
    {
        public string type { get; set; }
        public string value { get; set; }
    }

    public class MA_Territories
    {
        public List<string> include { get; set; }
    }

    public class MA_InterestedParty
    {
        public string role { get; set; }
        public string name { get; set; }
    }

    public class MA_Miscellaneous
    {
        public string sourceRef { get; set; }
    }

    public class MA_Track_List
    {
        public List<MA_Track> results { get; set; }
    }

    public class MA_BulkUploadPayload
    {
        public string bucket { get; set; }
        public string key { get; set; }      
        public string region { get; set; }
        public string trackId { get; set; } 
    }

    public class MA_BulkUploadStatus
    {
        public string bucket { get; set; }
        public string key { get; set; }
        public string region { get; set; }        
        public DateTime dateCreated { get; set; }
        public string error { get; set; }
        public string status { get; set; }
        public Guid trackId { get; set; }
    }

    public class MLEditMetadataJson
    {
        public string arranger { get; set; }
        public string composer { get; set; }
        public string performer { get; set; }
        public string isrc { get; set; }
        public string iswc { get; set; }
        public string musicorigin { get; set; }
        public string track_name { get; set; }
        public string publisher { get; set; }
        public string recLabel { get; set; }       

    }
}
