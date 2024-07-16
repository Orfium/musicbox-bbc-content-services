using System;
using System.Collections.Generic;
using System.Text;

namespace LabelProcess.Model
{
    public class ProblematicISRCsMatchedClearanceTracks
    {
        public string ClearancFormTrackID { get; set; }
        public string ClearancFormID { get; set; }
        public int? ClearanceFormAllocatedUser { get; set; }
        public string ClearanceFormRefNo { get; set; }
        public string ClearanceFormDeadline { get; set; }
        public int ClearanceStatus { get; set; }
        public string ISRC { get; set; }
        public string DHTrackID { get; set; }
        public string PRSWorkTunecode { get; set; }
        public string PRSWorkTitle { get; set; }
        public string PRSWorkPublishers { get; set; }
        public string PRSWorkWritors { get; set; }
        public string PRSSearchDatetime { get; set; }
        public string DHTuneCode { get; set; }
        public string MLID { get; set; }
        public string WSID { get; set; }      
        public string MatchingType { get; set; }
        public string MLTrackTitle { get; set; }
        public string MLComposer { get; set; }
        public string MLPublisher { get; set; }
        public string MLPerformer { get; set; }
        public string NewPRSWorkTunecode { get; set; }
        public string NewPRSWorkTitle { get; set; }
        public string NewPRSWorkPublishers { get; set; }
        public string NewPRSWorkWritors { get; set; }
        public string OldPRSWorkTitle { get; set; }
        public string OldPRSWorkPublishers { get; set; }
        public string OldPRSWorkWritors { get; set; }
    }
}
