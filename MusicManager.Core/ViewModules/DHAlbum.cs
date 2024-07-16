using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public class DHAlbum
    {
        public Guid? id { get; set; }
        public string artist { get; set; }
        public DHAArtwork artwork { get; set; }
        public string cLine { get; set; }
        public int? discs { get; set; }
        public List<DHValueType> identifiers { get; set; }
        public Guid? libraryId { get; set; }
        public DHAMiscellaneous miscellaneous { get; set; }
        public string name { get; set; }
        public string notes { get; set; }
        public string releaseDate { get; set; }
        public string subtitle { get; set; }
        public string uniqueId { get; set; }
        public string upc { get; set; }
        public List<DescriptiveData> descriptiveExtended { get; set; }
        public List<Tag> tagsExtended { get; set; }
        public Guid? versionId { get; set; }
        public int? year { get; set; }
        public string releaseYear { get; set; }
    }

    public class DHAlbumList
    {
        public List<DHAlbum> results { get; set; }
    }    


    public class DHAArtwork
    {
        public DateTime? dateCreated { get; set; }
        public string format { get; set; }
        public string size { get; set; }
    }

    public class DHAMiscellaneous
    {
        public string sourceRef { get; set; }
        public string dbpowerampDiscId { get; set; }
        public string musicBrainzReleaseGroupId { get; set; }
        public string sourceVersionId { get; set; }
    }   
}
