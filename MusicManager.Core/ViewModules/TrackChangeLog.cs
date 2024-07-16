using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class TrackChangeLog
    {
        public string Action { get; set; }
        public DateTime DateCreated { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public Guid? RefId { get; set; }
        public Guid? SourceRefId { get; set; }
        public string SourceFileName { get; set; }
    }

    public partial class UploadDesctiptiveRef : TrackChangeLog
    {
        public string AssetS3Id { get; set; }
        public string BucketName { get; set; }
        public long? Size { get; set; }
    }
}
