using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class EditAlbumMetadata
    {
        public Guid? library_id { get; set; }
        public Guid id { get; set; }
        public Guid? dh_album_id { get; set; }
        public Guid? UploadId { get; set; }
        public string album_title { get; set; }
        public string album_artist { get; set; }
        public string catalogue_number { get; set; }
        public string album_notes { get; set; }
        public string album_source_ref { get; set; }
        public int? album_discs { get; set; }
        public string album_release_date { get; set; }
        public string release_year { get; set; }
        public int prod_year { get; set; }
        public string album_subtitle { get; set; }
        public string upc { get; set; }
        public string org_album_admin_notes { get; set; }
        public List<Tag> album_orgTags { get; set; }
        public List<string> org_album_userTags { get; set; }
        public List<string> org_album_adminTags { get; set; }
        public string bbc_album_id { get; set; }

        public string cLine { get; set; }
        public List<Guid> selectedTracks { get; set; }
        public string artwork_url { get; set; }
        public Guid? version_id { get; set; }
    }

}
