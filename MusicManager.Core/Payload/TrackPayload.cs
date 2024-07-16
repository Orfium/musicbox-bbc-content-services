using Microsoft.AspNetCore.Http;
using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.Payload
{
    public partial class TrackPayload
    {
        public string createdBy { get; set; }
        public int sessionId { get; set; }
        public IFormFile asset { get; set; }
    }

    public partial class TrackMetadataPayload
    {
        public string sessionId { get; set; }
        public string trackTitle { get; set; }
        public Guid workspaceId { get; set; }
        public string musicOrigin { get; set; }
        public string assetKey { get; set; }
        public Guid mlTrackId { get; set; }
        public string s3Bucket { get; set; }
        public string extention { get; set; }
        public EditTrackMetadata metadataJson { get; set; }
        public string orgId { get; set; }

        // public string dhTrackJson { get; set; }
        // public string CreatedBy { get; set; }
    }

    public partial class TrackXMLPayload
    {
        public int sessionId { get; set; }
        public List<UploadTrackData> data { get; set; }
        public string userId { get; set; }
        public string orgId { get; set; }
    }

    public partial class UploadTrackData
    {
        public string xml { get; set; }
        public string trackName { get; set; }
        public string extention { get; set; }
        public string type { get; set; }
        public int? size { get; set; }
        public string artwork { get; set; }
        public string xmlHash { get; set; }
        
    }

    public partial class TrackXMLReturn
    {
        public bool exist { get; set; }
        public upload_track uploadTrack { get; set; }
        public string fileName { get; set; }
    }

    public partial class UploadObject
    {
        public AWSAccess S3Token { get; set; }
        public List<TrackXMLReturn> tracks { get; set; }
    }

    public partial class UploadTrackT1
    {
        public bool exist { get; set; }
        public upload_track uploadTrack { get; set; }
        public string fileName { get; set; }
        public byte[] artworkData { get; set; }
        public EditTrackMetadata editTrackMetadata { get; set; }
        public EditAlbumMetadata editAlbumMetadata { get; set; }
    }

    public partial class TrackCountPayload
    {
        public string refId { get; set; }
        public string type { get; set; }
        public string orgId { get; set; }
    }

    public partial class AddTrackToAlbumPayload
    {
        public string albumId { get; set; }
        public List<Guid> tracks { get; set; }
        public string userId { get; set; }
    }

    public partial class DeleteAlbumPayload
    {
        public Guid albumId { get; set; }
        public bool isTracksDelete { get; set; }
        public string userId { get; set; }
        public string orgId { get; set; }
        public string ws_id { get; set; }
    }

    public partial class TrackUpdatePayload
    {
        public EditTrackMetadata trackData { get; set; }
        public EditAlbumMetadata albumdata { get; set; }
        public DHAlbum dHAlbum { get; set; }
        public bool isAlbumEdit { get; set; }
        public string userId { get; set; }
        public string sessionId { get; set; }
        public string orgId { get; set; }

    }

    public partial class TrackCreatePayload
    {
        public EditTrackMetadata trackData { get; set; }
        public EditAlbumMetadata albumdata { get; set; }
        public string userId { get; set; }
        public string orgId { get; set; }
    }

    public partial class PplLabelPayload
    {
        public int id { get; set; }
        public string member { get; set; }
        public string label { get; set; }
        public string mlc { get; set; }
        public string userId { get; set; }
        public string source { get; set; }
        public string date_created { get; set; }
    }

    public partial class PriorApprovalPayload
    {
        public string id { get; set; }
        public string ice_mapping_code { get; set; }
        public string local_work_id { get; set; }
        public string tunecode { get; set; }
        public string iswc { get; set; }
        public string work_title { get; set; }
        public string composers { get; set; }
        public string publisher { get; set; }
        public string matched_isrc { get; set; }
        public string matched_dh_ids { get; set; }
        public string broadcaster { get; set; }
        public string artist { get; set; }
        public string writers { get; set; }
        public string org_id { get; set; }
        public string userId { get; set; }


    }

    public partial class AddPlayoutPayload
    {
        public int sessionId { get; set; }
        public string userId { get; set; }
        public string org_id { get; set; }
        public string type { get; set; }
        public List<string> selectedTracks { get; set; }


    }

    public partial class PlayoutPayload
    {
        public int track_count { get; set; }
        public int status { get; set; }
        public int type { get; set; }
        public string userId { get; set; }
        public string station_id { get; set; }
        public string org_id { get; set; }
        public int sessionId { get; set; }
        public string build_id { get; set; }
        public List<PublishTrackData> publishTrackData { get; set; }
    }

    public partial class PlayoutResponse
    {
        public int count { get; set; }
        public int sessionId { get; set; }
     
    }

    public partial class PlayoutTrackIdPayload
    {
        public List<string> trackIds { get; set; }
        public string org_id { get; set; }
        public string userId { get; set; }

    }

    public partial class PlayoutXMlDownloadPayload
    {       
        public string orgId { get; set; }
        public string userId { get; set; }
        public int playoutId { get; set; }
        public List<PlayoutDownloadTrack> tracks { get; set; }
        public string outputType { get; set; }
    }

    public class PlayoutDownloadTrack
    {
        public Guid trackId { get; set; }
        public Guid dhTrackId { get; set; }
        public string trackType { get; set; }
    }

    public partial class PublishTrackData
    {

        public string id { get; set; }
        public string track_id { get; set; }
        public string dh_track_id { get; set; }
        public string album_title { get; set; }
        public string label { get; set; }
        public string title { get; set; }
        public string isrc { get; set; }
        public string performer { get; set; }
        public string trackType { get; set; }
        public string artwork_url { get; set; }
        public bool do_update { get; set; }
        public float? duration { get; set; }
        public int asset_status { get; set; }


    }

    public partial class CTagPayload
    {
        public int id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public DateTime? date_created { get; set; }
        public string condition { get; set; }
        public int? created_by { get; set; }
        public string colour { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        public bool? is_restricted { get; set; }
        public string userId { get; set; }
        public string indicator { get; set; }
        public bool? display_indicator { get; set; }
        public int group_id { get; set; }

    }
    public partial class CTagArchivePayload
    {
        public List<int> ids { get; set; }
        public int status { get; set; }
        public string userId { get; set; }

    }

    public partial class CTagRuleDeletePayload
    {
        public List<CTagRuleListPayload> ctagPayload { get; set; }
        public string userId { get; set; }

    }


    public partial class CTagDeletePayload
    {
        public List<int> ids { get; set; }
        public string userId { get; set; }

    }

    public partial class CTagRuleListPayload
    {
        public Guid trackId { get; set; }
        public List<int> cTagIds { get; set; }

    }
    public partial class CTagTrackDeletePayload
    {
        public List<string> trackIds { get; set; }
        public string userId { get; set; }
    }



    public partial class CTagExtendedPayload
    {
        public int id { get; set; }
        public string userId { get; set; }
        public int c_tag_id { get; set; }
        public string name { get; set; }
        public string color { get; set; }
        public string description { get; set; }
        public DateTime? date_created { get; set; }
        public string rules { get; set; }
        public int? created_by { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        public bool? is_restricted { get; set; }
        public int status { get; set; }
        public string track_id { get; set; }
        public string notes { get; set; }

    }

    public partial class AlbumPayload
    {
        public Guid id { get; set; }
        public int session_id { get; set; }
        public Guid? dh_album_id { get; set; }
        public bool? modified { get; set; }
        public bool? artwork_uploaded { get; set; }
        public string artist { get; set; }
        public string album_name { get; set; }
        public string release_date { get; set; }
        public string metadata_json { get; set; }
        public DateTime date_created { get; set; }
        public int created_by { get; set; }
        public DateTime? date_last_edited { get; set; }
        public int? last_edited_by { get; set; }
        public string catalogue_number { get; set; }
        public string artwork { get; set; }
        public string rec_type { get; set; }
        public Guid? copy_source_album_id { get; set; }
        public Guid? copy_source_ws_id { get; set; }
    }

    public partial class TrackEditDeletePayload
    {
        public Guid track_id { get; set; }
        public string userId { get; set; }
        public string orgId { get; set; }
        public string source { get; set; }
        public string workspaceId { get; set; }
        public Guid? dhTrackId { get; set; }
    }

    public partial class AlbumkEditDeletePayload
    {
        public Guid albumId { get; set; }
        public string userId { get; set; }
        public string orgId { get; set; }
        public string source { get; set; }
        public string workspaceId { get; set; }
        public Guid prodId { get; set; }
    }

    public partial class DeleteTrackPayload
    {
        public List<string> track_ids { get; set; }
        public string userId { get; set; }
        public string orgId { get; set; }
        public string source { get; set; }
        public string workspaceId { get; set; }
        public Guid? dhTrackId { get; set; }

    }
    public partial class AlbumArtPayload
    {
        public Guid albumId { get; set; }
        public string _stream { get; set; }
        public string orgId { get; set; }
    }

    public partial class TrackReorderPayload
    {
        public int sourceIndex { get; set; }
        public Guid albumId { get; set; }
        public int destIndex { get; set; }
    }


    public partial class UpdateDHTrackPayload
    {
        public EditTrackMetadata trackMetadata { get; set; }
        public EditAlbumMetadata albumMetadata { get; set; }
        public DHTrack dHTrack { get; set; }
        public DHAlbum dHAlbum { get; set; }
        public bool isAlbumEdit { get; set; }
        public bool isPRS { get; set; }
        public string userId { get; set; }
        public string sessionId { get; set; }
        public Guid wsId { get; set; }
        public string orgId { get; set; }
    }

    public partial class MLCopyPayload
    {
        public string userId { get; set; }
        public Guid trackOrgId { get; set; }
        public Guid? dhTrackId { get; set; }
        public string orgId { get; set; }
    }

    public partial class ContentAlert
    {
        public Guid refId { get; set; }
        public string mediaType { get; set; }
        public int alertType { get; set; }
        public string alertNote { get; set; }
    }
    public partial class ContentAlertPayLoad
    {
        public string userId { get; set; }
        public List<ContentAlert> contentAlerts { get; set; }
        public string orgId { get; set; }
    }

    public partial class ResolveContentAlert
    {
        public Guid refId { get; set; }
        public string mediaType { get; set; }

    }
    public partial class ResolveContentAlertPayLoad
    {
        public string userId { get; set; }
        public List<ResolveContentAlert> contentAlerts { get; set; }
        public string orgId { get; set; }
    }


    public partial class UserPayload
    {
        public string user_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string role_id { get; set; }
        public string image_url { get; set; }
        public string org_id { get; set; }
    }

    public partial class CheckDatahubTracksPayload
    {
        public List<Guid> trackIds { get; set; }        
    }
    
}
