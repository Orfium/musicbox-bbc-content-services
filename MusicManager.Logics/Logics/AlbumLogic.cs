using Elasticsearch.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Extensions;
using MusicManager.Logics.Helper;
using MusicManager.Logics.ServiceLogics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class AlbumLogic : IAlbumLogic
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<DHTrackSync> _logger;
        private readonly IUploaderLogic _uploaderLogic;
        private readonly IElasticLogic _elasticLogic;

        public AlbumLogic(IUnitOfWork unitOfWork, IOptions<AppSettings> appSettings, ILogger<DHTrackSync> logger,
            IUploaderLogic uploaderLogic,IElasticLogic elasticLogic)
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
            _logger = logger;
            _uploaderLogic = uploaderLogic;
            _elasticLogic = elasticLogic;
        }

        public async Task<string> CreateAlbum(TrackUpdatePayload trackUpdatePayload)
        {
            string artWorkStatus = string.Empty;
            string status = string.Empty;
            var uploadSession = await _unitOfWork.UploadSession.CreateSession(trackUpdatePayload.userId, trackUpdatePayload.orgId);           
            IEnumerable<Guid> prevAlbumIdsDistinct = null;

            if (trackUpdatePayload?.albumdata?.selectedTracks?.Count() > 0)
                prevAlbumIdsDistinct = await _unitOfWork.Album.GetDistinctAlbumIdFromTrackIds(trackUpdatePayload.albumdata.selectedTracks);

            org_user org_User = await _unitOfWork.User.GetUserById(int.Parse(trackUpdatePayload.userId));
            if (org_User == null)
                org_User = new org_user() { user_id = int.Parse(trackUpdatePayload.userId)};
            

            trackUpdatePayload.albumdata.id = Guid.NewGuid();
            trackUpdatePayload.albumdata.UploadId = Guid.NewGuid();
           
            upload_album upload_Album = new upload_album()
            {
                album_name = trackUpdatePayload.albumdata.album_title,
                artist = trackUpdatePayload.albumdata.album_artist,
                date_created = DateTime.Now,
                modified = false,
                created_by = trackUpdatePayload.userId != null ? Convert.ToInt32(trackUpdatePayload.userId) : 0,
                metadata_json = JsonConvert.SerializeObject(trackUpdatePayload.albumdata, new JsonSerializerSettings()),
                session_id = (int)uploadSession.id,
                id = trackUpdatePayload.albumdata.id,
                upload_id = trackUpdatePayload.albumdata.UploadId,
                date_last_edited = DateTime.Now                
            };

            DHAlbum dhAlbum = trackUpdatePayload.albumdata.CreateDHAlbumFromEditAlbumMetadata((Guid)trackUpdatePayload.albumdata.UploadId, org_User);

            DHAlbum createdAlbum = await _unitOfWork.MusicAPI.CreateAlbum(_appSettings.Value.MasterWSId, dhAlbum);

            if (createdAlbum != null)
            {
                upload_Album.dh_album_id = createdAlbum.id;
                trackUpdatePayload.albumdata.dh_album_id = (Guid)createdAlbum.id;

                workspace_org workspaceOrg = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(Guid.Parse(_appSettings.Value.MasterWSId), trackUpdatePayload.orgId);

                album_org album_Org = new album_org()
                {
                    id = (Guid)upload_Album.upload_id,
                    original_album_id = (Guid)createdAlbum.id,
                    archive = false,
                    created_by = org_User.user_id,
                    last_edited_by = org_User.user_id,
                    ml_status = (int)enMLStatus.Live,
                    org_id = trackUpdatePayload.orgId,
                    restricted = false,
                    org_workspace_id = workspaceOrg.org_workspace_id,
                    api_result_id = 0
                };
                await _unitOfWork.TrackOrg.InsertUpdateAlbumOrg(new List<album_org>() { album_Org });
            }                

            if (!string.IsNullOrEmpty(trackUpdatePayload.albumdata.artwork_url))
            {
                artWorkStatus = await _unitOfWork.UploadAlbum.UploadArtWork(upload_Album.dh_album_id, trackUpdatePayload.albumdata.artwork_url);
            }
            if (!string.IsNullOrEmpty(artWorkStatus))
            {
                upload_Album.artwork = artWorkStatus;
                trackUpdatePayload.albumdata.artwork_url = artWorkStatus;
                upload_Album.metadata_json = JsonConvert.SerializeObject(trackUpdatePayload.albumdata, new JsonSerializerSettings());
            }
            await _unitOfWork.UploadAlbum.CreateAlbum(upload_Album);

            //Updating Tracks
            if (trackUpdatePayload.albumdata.selectedTracks != null)
            {
                foreach (var trackId in trackUpdatePayload.albumdata.selectedTracks)
                {
                    await AddTracks(trackId, trackUpdatePayload.albumdata, null, dhAlbum, upload_Album);
                }
            }

            Product product = new Product()
            {
                Id = (Guid)createdAlbum.id,
                ArtworkUri = artWorkStatus,
                Name = createdAlbum.name,
                Artist = createdAlbum.artist,
                Notes = createdAlbum.notes,
                NumberOfDiscs = createdAlbum.discs.ToString(),
                SubName = createdAlbum.subtitle,
                CLine = createdAlbum.cLine,
                Identifiers = new Dictionary<string, string>(),
                VersionId = createdAlbum.versionId
            };

            if (!string.IsNullOrEmpty(createdAlbum.upc))
                product.Identifiers.Add("upc", createdAlbum.upc);

            if (!string.IsNullOrEmpty(trackUpdatePayload.albumdata.catalogue_number))
                product.Identifiers.Add("catalogue_number", trackUpdatePayload.albumdata.catalogue_number);

            if (!string.IsNullOrEmpty(trackUpdatePayload.albumdata.album_release_date)
                && DateTime.TryParse(trackUpdatePayload.albumdata.album_release_date, out DateTime dateTime))
            {
                product.ReleaseDate = dateTime;
            }

            await _unitOfWork.AlbumAPIResults.Insert(new log_album_api_results()
            {
                album_id = (Guid)createdAlbum.id,
                api_call_id = 0,
                metadata = JsonConvert.SerializeObject(product, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                date_created = DateTime.Now,
                date_modified = DateTime.Now,
                deleted = false,
                session_id = 0,
                workspace_id = Guid.Parse(_appSettings.Value.MasterWSId),
                //version_id = createdAlbum.versionId,
                received = 0
            });

            if (createdAlbum != null && !string.IsNullOrEmpty(artWorkStatus))
            {
                status = "Success";
            }
            else if (createdAlbum == null && !string.IsNullOrEmpty(artWorkStatus))
            {
                status = "Album Failed";
            }
            else if (createdAlbum != null && string.IsNullOrEmpty(artWorkStatus))
            {
                status = "Artwork Failed";
            }
            else
            {
                status = "Failed";
            }
           
            if (prevAlbumIdsDistinct != null)
            {
                _ = Task.Run(() => _unitOfWork.Album.CheckAndUpdatePreviousAlbumOrgs(prevAlbumIdsDistinct.ToList())).ConfigureAwait(false);
            }          

            return status;
        }

        public async Task<string> UpdateAlbum(TrackUpdatePayload albumPayload)
        {
            string status = string.Empty;
            string artWorkStatus = string.Empty;
            ml_master_album ml_Master_Album = null;
            Guid mlVersionId = Guid.NewGuid();
            DHAlbum dhAlbum = null;

            org_user org_User = await _unitOfWork.User.GetUserById(int.Parse(albumPayload.userId));
            if (org_User == null)
                org_User = new org_user() { user_id = int.Parse(albumPayload.userId) };

            if (albumPayload.dHAlbum == null)
            {
                ml_Master_Album = await _unitOfWork.Album.GetMlMasterAlbumById((Guid)albumPayload.albumdata.dh_album_id);

                if (ml_Master_Album != null)
                {
                    Product product = JsonConvert.DeserializeObject<Product>(ml_Master_Album.metadata);
                    albumPayload.dHAlbum = CommonHelper.CreateDHAlbumFromProduct(product, enMLUploadAction.ML_ALBUM_ADD.ToString(), ml_Master_Album.library_id, null);
                }
                else
                {
                    albumPayload.albumdata.id = (Guid)albumPayload.albumdata.dh_album_id;
                    albumPayload.dHAlbum = albumPayload.albumdata.CreateDHAlbumFromEditAlbumMetadata(albumPayload.albumdata.id, org_User);                    
                }
            }

            //update datahub 
            if (albumPayload.albumdata.dh_album_id != null)
            {
                dhAlbum = albumPayload.dHAlbum.UpdateDHAlbumFromEditAlbumMetadata(albumPayload.albumdata);

                dhAlbum = await _unitOfWork.MusicAPI.UpdateAlbum(dhAlbum.id.ToString(), dhAlbum);

                if (dhAlbum != null)
                {
                    albumPayload.albumdata.artwork_url = albumPayload.albumdata.artwork_url?.Replace("&amp;", "&");

                    if (!string.IsNullOrEmpty(albumPayload.albumdata.artwork_url))
                    {
                        artWorkStatus = await _unitOfWork.UploadAlbum.UploadArtWork(dhAlbum.id, albumPayload.albumdata.artwork_url);
                    }

                    //Update upload_album table
                    var upload_album = await _unitOfWork.UploadAlbum.GetAlbumByProductId((Guid)albumPayload.albumdata.dh_album_id);
                    albumPayload.albumdata.artwork_url = artWorkStatus;
                    if (upload_album != null)
                    {
                        upload_album.metadata_json = JsonConvert.SerializeObject(albumPayload.albumdata, new JsonSerializerSettings());
                        upload_album.album_name = albumPayload.albumdata != null ? albumPayload.albumdata.album_title : "";
                        upload_album.artist = albumPayload.albumdata.album_artist;
                        upload_album.date_last_edited = DateTime.Now;
                        upload_album.last_edited_by = albumPayload.userId != null ? Convert.ToInt32(albumPayload.userId) : 0;
                        if (!string.IsNullOrEmpty(artWorkStatus))
                        {
                            upload_album.artwork = artWorkStatus;
                        }
                        await _unitOfWork.UploadAlbum.UpdateAlbum(upload_album);
                    }

                    await _uploaderLogic.UpdateAlbumIndex(albumPayload.albumdata, (Guid)albumPayload.albumdata.dh_album_id);

                    //UDYOGA

                    await UpdateAlbumOrgWhenEdit(albumPayload.albumdata, org_User, null);
                    //await _unitOfWork.Album.UpdateOrgData(albumPayload.albumdata, albumPayload.orgId);

                    IEnumerable<ml_master_track> ml_Master_Tracks = await _unitOfWork.MLMasterTrack.GetMasterTracksByAlbumId((Guid)albumPayload.albumdata.dh_album_id);
                    foreach (var item in ml_Master_Tracks)
                    {
                        track_org track_Org = await _unitOfWork.TrackOrg.GetTrackOrgByDhTrackIdAndOrgId(item.track_id, albumPayload.orgId);

                        if (track_Org != null) {
                            Track _trackDoc = JsonConvert.DeserializeObject<Track>(item.metadata);

                            DHTrack dhTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, item.ext_sys_ref);
                            dhTrack.uniqueId = track_Org.id.ToString();

                            EditTrackMetadata mLTrackMetadataEdit = dhTrack.CreateEditTrack(dhTrack.title);
                            mLTrackMetadataEdit.version_id = mlVersionId;

                            long updatedEpochTime = CommonHelper.GetCurrentUtcEpochTimeMicroseconds();

                            MLTrackDocument mLTrackDocument = await _uploaderLogic.UpdateMasterTrackAndAlbumFromUploads(item.track_id, _trackDoc,
                               mLTrackMetadataEdit,
                               albumPayload.albumdata,
                               true, updatedEpochTime, null);

                            await _elasticLogic.UpdateIndex(mLTrackDocument);
                        }                        
                    }

                    //update upload tracks
                    var tracks = await _unitOfWork.UploadTrack.GetTracksFromAlbumId((Guid)albumPayload.albumdata.dh_album_id);
                    foreach (var track in tracks)
                    {
                        EditTrackMetadata trackMetadata = JsonConvert.DeserializeObject<EditTrackMetadata>(track.metadata_json);
                        trackMetadata.artwork_url = artWorkStatus;
                        track.metadata_json = JsonConvert.SerializeObject(trackMetadata, new JsonSerializerSettings());                     

                        await _unitOfWork.UploadTrack.UpdateTrackDhAlbumMetaData(track);
                    }
                    status = "Success";
                }
                else {
                    status = "Failed";
                }
            }

            log_user_action actionLog = new log_user_action
            {
                data_type = "",
                date_created = DateTime.Now,
                user_id = Convert.ToInt32(albumPayload.userId),
                org_id = albumPayload.orgId == null ? "" : albumPayload.orgId,
                data_value = "",
                action_id = (int)enActionType.UPDATE_ALBUM,
                ref_id = Guid.Empty, // ws id
                status = 1
            };
            _ = Task.Run(() => _unitOfWork.ActionLogger.Log(actionLog)).ConfigureAwait(false);           

            return status;

        }

        public async Task<string> DeleteAlbum(DeleteAlbumPayload albumPayload, enWorkspaceType wsType)
        {
            List<upload_track> album_tracks = null;
            //DHTrack dhTrack = new DHTrack();
            string status = string.Empty;

            upload_album uploadAlbum = await _unitOfWork.UploadAlbum.GetAlbumById(albumPayload.albumId);

            if (uploadAlbum == null)
                return "Failed";            

            album_tracks = await _unitOfWork.UploadTrack.GetTracksFromAlbumId(albumPayload.albumId);

            if (album_tracks?.Count > 0)
            {
                if (albumPayload.isTracksDelete)
                {

                    foreach (var track in album_tracks)
                    {
                        //delete from dh
                        var trackdelResponse = await _unitOfWork.MusicAPI.DeleteTrack(track.dh_track_id.ToString());

                        //delete tracks from album
                        status = await _unitOfWork.UploadAlbum.RemoveTrackFromAlbum(track.id, wsType);

                        upload_track uploadTrack = await _unitOfWork.UploadTrack.GetUploadTrackByUploadId(track.id);

                        if (uploadTrack != null)
                            await _elasticLogic.DeleteTracks(new Guid[1] { (Guid)uploadTrack.upload_id });
                    }

                }
                else
                {                    
                    foreach (upload_track track in album_tracks)
                    {
                        //update dh tracks (Update tracks from album)
                        ml_master_track ml_master_track = await _unitOfWork.MLMasterTrack.GetMaterTrackById((Guid)track.dh_track_id);
                        if (ml_master_track != null && ml_master_track.metadata != null)
                        {
                            Track _trackDoc = JsonConvert.DeserializeObject<Track>(ml_master_track.metadata);
                            DHTrack dhTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, ml_master_track.ext_sys_ref);
                            dhTrack.id = track.dh_track_id;
                            dhTrack.albumId = null;

                            long updatedEpochTime = CommonHelper.GetCurrentUtcEpochTimeMicroseconds();

                            //--- Update track elastic index
                            EditTrackMetadata mLTrackMetadataEdit = dhTrack.CreateEditTrack(dhTrack.title);
                            mLTrackMetadataEdit.version_id = Guid.NewGuid();

                            MLTrackDocument mLTrackDocument = await _uploaderLogic.UpdateMasterTrackAndAlbumFromUploads(ml_master_track.track_id, _trackDoc,
                               mLTrackMetadataEdit,
                               null,
                               true, updatedEpochTime,null);

                            await _elasticLogic.UpdateIndex(mLTrackDocument);
                           
                            DHTrack check = await _unitOfWork.MusicAPI.UpdateTrack(dhTrack.id.ToString(), dhTrack);

                            if (check != null)
                            {
                                //Update tracks from upload album
                                var track_metadata = JsonConvert.DeserializeObject<EditTrackMetadata>(track.metadata_json);
                                track_metadata.artwork_url = "";
                                if (await _unitOfWork.UploadTrack.RemoveAlbumFromTrack(track.id, track_metadata))
                                {
                                    status = "Success";
                                }
                                else
                                {
                                    status = "Failed";
                                }
                            }                                
                        }
                    }                    
                }                
            }

            //-- Delete datahub album
            if (uploadAlbum.dh_album_id != null)
            {
                HttpWebResponse albumResponse = await _unitOfWork.MusicAPI.DeleteAlbum(uploadAlbum.dh_album_id.ToString());
                if (albumResponse.StatusCode == HttpStatusCode.OK ||
                albumResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    //-- Delete upload album and master album
                    status = await _unitOfWork.UploadAlbum.RemoveUploadAlbum(uploadAlbum);
                }
                else
                {
                    status = "Failed";
                    return status;
                }
            }
            else
            {
                //-- Delete upload album and master album
                status = await _unitOfWork.UploadAlbum.RemoveUploadAlbum(uploadAlbum);
            }
            _logger.LogInformation("DeleteAlbum {@AlbumId} : {@UserId} : {@Status}", uploadAlbum.dh_album_id, albumPayload.userId, status);
            return status;
        }

        public async Task<int> AddTracksToAlbum(AddTrackToAlbumPayload albumPayload)
        {
            DHAlbum dHAlbum = new DHAlbum();   
          
            int trackCount = 0;

            IEnumerable<Guid> prevAlbumIdsDistinct = null;

            if (albumPayload?.tracks.Count() > 0)
                prevAlbumIdsDistinct = await _unitOfWork.Album.GetDistinctAlbumIdFromTrackIds(albumPayload?.tracks);

            upload_album uploadAlbum = await _unitOfWork.UploadAlbum.GetAlbumById(Guid.Parse(albumPayload.albumId));
            ml_master_album mlMasterAlbum = await _unitOfWork.Album.GetMlMasterAlbumById((Guid)uploadAlbum.dh_album_id);

            EditAlbumMetadata albumMetaData = JsonConvert.DeserializeObject<EditAlbumMetadata>(uploadAlbum.metadata_json);
            albumMetaData.dh_album_id = mlMasterAlbum.album_id;

            dHAlbum = dHAlbum.UpdateDHAlbumFromEditAlbumMetadata(albumMetaData);
            dHAlbum.id = mlMasterAlbum.album_id;

            albumMetaData.artwork_url = uploadAlbum.artwork;

            foreach (var item in albumPayload.tracks)
            {
                await AddTracks(item, albumMetaData, mlMasterAlbum, dHAlbum, uploadAlbum);
            }            

            _logger.LogInformation("Add Tracks to Album {@AlbumId} : {@UserId} : {@trackIds}", mlMasterAlbum.album_id, albumPayload.userId, albumPayload.tracks);
            
            _ = Task.Run(() => _unitOfWork.Album.CheckAndUpdatePreviousAlbumOrgs(prevAlbumIdsDistinct.ToList())).ConfigureAwait(false);

            return trackCount;
        }

        private async Task AddTracks(Guid mlTrackId, EditAlbumMetadata albumMetaData, ml_master_album mlMasterAlbum, DHAlbum dHAlbum, upload_album uploadAlbum)
        {
            ml_master_track mlMasterTrack = null;
            upload_track uploadTrack = await _unitOfWork.UploadTrack.GetUploadTrackByUploadId(mlTrackId);
            if (uploadTrack != null && uploadTrack.dh_track_id != null)
            {
                mlMasterTrack = await _unitOfWork.MLMasterTrack.GetMaterTrackById((Guid)uploadTrack.dh_track_id);

                if (mlMasterTrack != null) {
                    Track _trackDoc = JsonConvert.DeserializeObject<Track>(mlMasterTrack.metadata);
                    if (mlMasterAlbum != null) //--- Update Product details from ml_master_album metadata
                    {
                        _trackDoc.TrackData.Product = JsonConvert.DeserializeObject<Product>(mlMasterAlbum.metadata);
                    }
                    else
                    {
                        _trackDoc.TrackData.Product = _uploaderLogic.CreateProductFromEditAlbumMetadata(albumMetaData);
                    }


                    DHTrack dhTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, mlMasterTrack.ext_sys_ref);

                    DHTrack updatedTrack = await _unitOfWork.MusicAPI.UpdateTrack(dhTrack.id.ToString(), dhTrack);

                    if (updatedTrack != null)
                    {
                        await _uploaderLogic.ChangeTrackAlbum((Guid)updatedTrack.versionId, _trackDoc, mlTrackId);

                        await _unitOfWork.UploadTrack.UpdateByHDTrack(dhTrack, dHAlbum, _trackDoc.TrackData.Product.ArtworkUri, uploadTrack,uploadAlbum);
                    }
                }                       
            }
        }

        public async Task<DHTrackEdit> CreateEditAlbum(AlbumkEditDeletePayload albumkEditDeletePayload)
        {
            DHTrackEdit dHTrackEdit = new DHTrackEdit();
            upload_album uploadAlbum = new upload_album();
            Product product = null;
            DHAlbum dHAlbum = null;
            EditAlbumMetadata editAlbumMetadata = null;

            album_org album_Org = await _unitOfWork.Album.GetAlbumOrgById(albumkEditDeletePayload.albumId);
            if (album_Org == null)
            {
                album_Org = await _unitOfWork.Album.GetAlbumOrgByDhAlbumIdAndOrgId(albumkEditDeletePayload.prodId, albumkEditDeletePayload.orgId);
            }
            ml_master_album ml_Master_Album = await _unitOfWork.Album.GetMlMasterAlbumById(albumkEditDeletePayload.prodId);

            if (ml_Master_Album != null)
            {
                product = JsonConvert.DeserializeObject<Product>(ml_Master_Album.metadata);
                dHAlbum = CommonHelper.CreateDHAlbumFromProduct(product, enMLUploadAction.ML_ALBUM_ADD.ToString(), ml_Master_Album.library_id, null);
                editAlbumMetadata = dHAlbum.CreateEditAlbum();
                editAlbumMetadata.artwork_url = product.ArtworkUri;
                editAlbumMetadata.dh_album_id = product.Id;
                editAlbumMetadata.id = product.Id;
            }
            else {
                uploadAlbum = await _unitOfWork.UploadAlbum.GetAlbumByProductId(albumkEditDeletePayload.prodId);
                editAlbumMetadata = JsonConvert.DeserializeObject<EditAlbumMetadata>(uploadAlbum.metadata_json);
                editAlbumMetadata.artwork_url = uploadAlbum.artwork;
            }            
            
            dHTrackEdit.albumMetadata = editAlbumMetadata;            

            if (album_Org != null)
            {
                dHTrackEdit.albumMetadata.dh_album_id = album_Org.original_album_id;
                dHTrackEdit.albumMetadata.id = album_Org.id;
            }               


            //if (album_Org?.org_data != null)
            //{                
            //    dHTrackEdit.albumMetadata.org_album_adminTags = new List<string>();
            //    dHTrackEdit.albumMetadata.org_album_userTags = new List<string>();
            //    dHTrackEdit.albumMetadata.album_orgTags = JsonConvert.DeserializeObject<List<Tag>>(album_Org.org_data);
            //    if (dHTrackEdit.albumMetadata.album_orgTags?.Count > 0)
            //    {
            //        dHTrackEdit.albumMetadata.org_album_admin_notes = dHTrackEdit.albumMetadata.album_orgTags.FirstOrDefault(x => x.Type == enAdminTypes.BBC_ADMIN_NOTES.ToString()).Value;
            //        foreach (var tag in dHTrackEdit.albumMetadata.album_orgTags)
            //        {
            //            if (tag.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
            //            {
            //                dHTrackEdit.albumMetadata.org_album_adminTags.Add(tag.Value);
            //            }
            //            if (tag.Type == string.Format("{0}_{1}", enAdminTypes.BBC_USER_TAG.ToString(), albumkEditDeletePayload.userId))
            //            {
            //                dHTrackEdit.albumMetadata.org_album_userTags.Add(tag.Value);
            //            }
            //        }
            //    }
            //}
            return dHTrackEdit;
        }

        public async Task<List<upload_track>> ReorderAlbumTracks(TrackReorderPayload trackReorderPayload)
        {
            try
            {
                List<upload_track> uploadTracks = await _unitOfWork.UploadTrack.GetTracksFromAlbumId(trackReorderPayload.albumId);

                upload_track sourceItem = uploadTracks[trackReorderPayload.sourceIndex];
                upload_track destItem = uploadTracks[trackReorderPayload.destIndex];

                if (sourceItem.disc_no == destItem.disc_no) {
                    uploadTracks.RemoveAt(trackReorderPayload.sourceIndex);
                    uploadTracks.Insert(trackReorderPayload.destIndex, sourceItem);

                    uploadTracks = uploadTracks.Where(a=>a.disc_no== sourceItem.disc_no).ToList();

                    uploadTracks = uploadTracks.Select((x, i) => { x.position = i + 1; return x; }).ToList();

                    await _unitOfWork.UploadTrack.ReorderUploadTracks(uploadTracks);
                }                
                return uploadTracks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReorderAlbumTracks > Album Id > " + trackReorderPayload.albumId);
                return null;
            }
        }

        public async Task UpdateAlbumOrgWhenEdit(EditAlbumMetadata editAlbumMetadata, org_user orgUser,Guid? trackId)
        {
            album_org albumOrg = await _unitOfWork.Album.GetAlbumOrgByDhAlbumIdAndOrgId((Guid)editAlbumMetadata.dh_album_id, orgUser.org_id);

            if (albumOrg != null)
            {
                List<TrackChangeLog> trackChange = albumOrg.change_log == null ? new List<TrackChangeLog>() : JsonConvert.DeserializeObject<List<TrackChangeLog>>(albumOrg.change_log);
                trackChange.Add(new TrackChangeLog()
                {
                    Action = enTrackChangeLogAction.EDIT.ToString(),
                    DateCreated = DateTime.Now,
                    UserId = orgUser.user_id,
                    UserName = orgUser.first_name + " " + orgUser.last_name
                });

                albumOrg.change_log = JsonConvert.SerializeObject(trackChange);
                albumOrg.org_data = JsonConvert.SerializeObject(editAlbumMetadata.album_orgTags, new JsonSerializerSettings());

                await _unitOfWork.Album.UpdateOrgData(albumOrg);

                await _unitOfWork.TrackOrg.UpdateTrackOrgByAlbumId(albumOrg.original_album_id, trackId);
            }
        }

        public async Task ClearEmptyUploadAlbums(IEnumerable<Guid> albumIds)
        {
            int? trackCount = 0;
            foreach (var item in albumIds)
            {
                trackCount = await _unitOfWork.UploadTrack.GetTrackCountByAlbumId(item);
                if (trackCount == 0) {

                    upload_album uploadAlbum = await _unitOfWork.UploadAlbum.GetAlbumById(item);
                    if (uploadAlbum != null) {
                        await _unitOfWork.UploadAlbum.DeleteUploadAlbumAndDeleteFromDatahub(uploadAlbum);
                        await _elasticLogic.DeleteAlbum(uploadAlbum.id);
                    }                   
                }
            }
        }

        public async Task UpdateRecordLabelOfAllAlbumTracks(Guid albumId, string recordLabel, Guid updatedTrackId,string orgId, EditAlbumMetadata editAlbumMetadata)
        {
            IEnumerable<ml_master_track> ml_Master_Tracks = await _unitOfWork.MLMasterTrack.GetMasterTracksByAlbumId(albumId);
            foreach (var item in ml_Master_Tracks)
            {
                if (updatedTrackId != item.track_id)
                {
                    track_org track_Org = await _unitOfWork.TrackOrg.GetTrackOrgByDhTrackIdAndOrgId(item.track_id, orgId);

                    Track _trackDoc = JsonConvert.DeserializeObject<Track>(item.metadata);

                    DHTrack dhTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, item.ext_sys_ref);


                    if (dhTrack.interestedParties?.Count() > 0)
                        dhTrack.interestedParties.RemoveAll(a => a.role == enIPRole.record_label.ToString());

                    if (!string.IsNullOrEmpty(recordLabel))
                    {
                        dhTrack.interestedParties.Add(new DHTInterestedParty()
                        {
                            name = recordLabel.ReplaceSpecialCodes(),
                            role = enIPRole.record_label.ToString()
                        });
                    }

                    //--- Update Datahub
                    await _unitOfWork.MusicAPI.UpdateTrack(item.track_id.ToString(), dhTrack);

                    EditTrackMetadata mLTrackMetadataEdit = dhTrack.CreateEditTrack(dhTrack.title);
                    mLTrackMetadataEdit.version_id = Guid.NewGuid();
                    mLTrackMetadataEdit.rec_label = recordLabel;

                    long updatedEpochTime = CommonHelper.GetCurrentUtcEpochTimeMicroseconds();

                    MLTrackDocument mLTrackDocument = await _uploaderLogic.UpdateMasterTrackAndAlbumFromUploads(item.track_id, _trackDoc,
                       mLTrackMetadataEdit,
                       editAlbumMetadata,
                       true, updatedEpochTime, null);

                    await _elasticLogic.UpdateIndex(mLTrackDocument);
                }

            }
        }
    }
}
