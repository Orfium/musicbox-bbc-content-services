using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class AlbumAPIObj
    {
        public Guid albumId { get; set; }
        public Guid? libraryId { get; set; }       
        public Guid versionId { get; set; }
        public Guid workspaceId { get; set; }
        public bool deleted { get; set; }
        public dynamic value { get; set; }
        public DateTime dateCreated { get; set; }
        public DateTime dateModified { get; set; }
    }

    public class AlbumAPIResponce
    {
        public nextPageToken nextPageToken { get; set; }
        public List<AlbumAPIObj> results { get; set; }
    }    
}
