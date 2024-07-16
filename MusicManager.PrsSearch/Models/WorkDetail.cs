using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.PrsSearch.Models
{
    public class ShareRights
    {
        public string RightOwnerName { get; set; }
        public string RightOwnerCae { get; set; }
        public bool RepresentedByPrs { get; set; }

        public ShareRights(string owner, string cae, bool isPrs)
        {
            RightOwnerName = owner;
            RightOwnerCae = cae;
            RepresentedByPrs = isPrs;
        }
    }

    public class WorkDetail
    {
        //---- Don't delete this comment - UDYOGA

        //public string TuneCode { get; set; }
        //public string[] Iswc { get; set; }
        //public string[] LibraryCatNoArray { get; set; }
        //public string WorkType { get; set; }
        //public string[] TitleArray { get; set; }
        //public List<CommonFunctions.IpData> WriterArrayList { get; set; }
        //public List<CommonFunctions.ArtistData> ArtistsArray { get; set; }
        //public List<CommonFunctions.IpData> PublisherArrayList { get; set; }
        //public string NoOfArtists { get; set; }
        //public List<ShareRights> OwnersMechArray { get; set; }
        //public List<ShareRights> OwnersPerfArray { get; set; }
    }
}
