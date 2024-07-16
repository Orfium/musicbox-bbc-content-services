using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class TrackAPIObj
    {
        public Guid trackId { get; set; }
        public Guid? libraryId { get; set; }
        public Guid? albumId { get; set; }
        public Guid versionId { get; set; }
        public Guid workspaceId { get; set; }
        public long received { get; set; }
        public long arid { get; set; }
        public bool deleted { get; set; }
        public dynamic value { get; set; }
    }

    public class TrackAPIResponce
    {
        public nextPageToken nextPageToken { get; set; }
        public List<TrackAPIObj> results { get; set; }
    }

    public class pageToken
    {
        public string dateModified { get; set; }
        public string versionId { get; set; }
    }

    public class nextPageToken
    {
        public string dateModified { get; set; }
        public string versionId { get; set; }
    }

    public class SearchAPIResponse
    {
        public List<SAResults> results { get; set; }
    }

    public class SAResults
    {
        public Guid id { get; set; }
        public dynamic metadata { get; set; }
        public string originalUrl { get; set; }
        public string previewUrl { get; set; }
    }   
}
