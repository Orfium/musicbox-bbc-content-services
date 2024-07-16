using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Extensions;
using MusicManager.Logics.Helper;
using MusicManager.Logics.ServiceLogics;
using Newtonsoft.Json;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{

    public class MlMasterTrackLogic : IMlMasterTrackLogic
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IAWSS3Repository _aWSS3Repositor;
        private readonly IUploaderLogic _uploaderLogic;
        private readonly IElasticLogic _elasticLogic;

        public MlMasterTrackLogic(IUnitOfWork unitOfWork, 
            IOptions<AppSettings> appSettings, 
            IAWSS3Repository AWSS3Repositor,
            IUploaderLogic uploaderLogic,
            IElasticLogic elasticLogic)
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
            _aWSS3Repositor = AWSS3Repositor;
            _uploaderLogic = uploaderLogic;
            _elasticLogic = elasticLogic;
        }

        public async Task<EditTrackMetadata> MakeMLCopy(MLCopyPayload mLCopyPayload)
        {
            try
            {
                MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocById(mLCopyPayload.trackOrgId);
                if (mLTrackDocument == null || mLTrackDocument.dhTrackId == null)
                    return null;

                upload_track upload_Track = null;
                upload_album upload_Album = null;
                ml_master_track ml_Master_Track = null;
                ml_master_album mlMasterAlbum = null;
                DHTrack dhTrack = null;
                DHAlbum dHAlbum = null;

                EditAlbumMetadata editAlbumMetadata = null;
                EditTrackMetadata editTrackMetadata = null;

                Guid albumUploadId = Guid.NewGuid();
                Guid trackUploadId = Guid.NewGuid();

                Guid copyDHTrackId = Guid.NewGuid();
                Guid copyDHAlbumId = Guid.NewGuid();

                bool albumUpload = false;

                string artworkUrl = "";
                org_user orgUser = await _unitOfWork.User.GetUserById(int.Parse(mLCopyPayload.userId));

                var upload_Session = await _unitOfWork.UploadSession.CreateSession(mLCopyPayload.userId, mLCopyPayload.orgId);

                ml_Master_Track = await _unitOfWork.MLMasterTrack.GetMaterTrackById((Guid)mLTrackDocument.dhTrackId);

                if (ml_Master_Track != null)
                {
                    mLCopyPayload.dhTrackId = ml_Master_Track.track_id;

                    Track _trackDoc = JsonConvert.DeserializeObject<Track>(ml_Master_Track.metadata);

                    if (ml_Master_Track.album_id != null)
                    {
                        mlMasterAlbum = await _unitOfWork.Album.GetMlMasterAlbumById((Guid)ml_Master_Track.album_id);
                        if (mlMasterAlbum != null)
                        {
                            _trackDoc.TrackData.Product = JsonConvert.DeserializeObject<Product>(mlMasterAlbum.metadata);
                        }
                    }

                    if (_trackDoc.TrackData.Product != null)
                    {
                        string catNo = "";

                        _trackDoc.TrackData.Product.Identifiers?.TryGetValue("catalogue_number", out catNo);

                        dHAlbum = CommonHelper.CreateDHAlbumFromProduct(_trackDoc.TrackData.Product, enMLUploadAction.ML_ALBUM_COPY.ToString(), null, albumUploadId);
                        editAlbumMetadata = dHAlbum.CreateEditAlbum();

                        artworkUrl = _trackDoc.TrackData.Product.ArtworkUri;

                        upload_Album = await _unitOfWork.UploadAlbum.GetAlbumByCopySourceId((Guid)dHAlbum.id);

                        if (upload_Album == null)
                        {
                            upload_Album = new upload_album()
                            {
                                id = copyDHAlbumId,
                                copy_source_album_id = (Guid)dHAlbum.id,
                                copy_source_ws_id = ml_Master_Track.workspace_id,
                                album_name = dHAlbum.name,
                                artist = dHAlbum.artist,
                                artwork = artworkUrl,
                                catalogue_number = catNo,
                                created_by = int.Parse(mLCopyPayload.userId),
                                date_created = DateTime.Now,
                                date_last_edited = DateTime.Now,
                                last_edited_by = int.Parse(mLCopyPayload.userId),
                                rec_type = enUploadRecType.COPY.ToString(),
                                session_id = (int)upload_Session.id,
                                metadata_json = JsonConvert.SerializeObject(editAlbumMetadata, new JsonSerializerSettings()),
                                artwork_uploaded = true,
                                modified = false,
                                release_date = dHAlbum.releaseDate,
                                upload_id = albumUploadId
                                //upc = editAlbumMetadata.upc,

                            };

                            await _unitOfWork.UploadAlbum.CreateAlbum(upload_Album);
                        }

                        if (upload_Album?.dh_album_id == null)
                        {
                            dHAlbum.id = copyDHAlbumId;

                            TrackChangeLog albumChangeLog = new TrackChangeLog()
                            {
                                Action = enTrackChangeLogAction.COPY.ToString(),
                                UserId = orgUser.user_id,
                                DateCreated = DateTime.Now,
                                UserName = orgUser.first_name != null ? orgUser.first_name + " " + orgUser.last_name : "",
                                RefId = albumUploadId,
                                SourceRefId = _trackDoc.TrackData.Product.Id
                            };

                            dHAlbum.descriptiveExtended = new List<DescriptiveData>();
                            dHAlbum.descriptiveExtended.Add(new DescriptiveData()
                            {
                                DateExtracted = DateTime.Now,
                                Source = enDescriptiveExtendedSource.ML_COPY.ToString(),
                                Type = enDescriptiveExtendedType.copy_source_album_id.ToString(),
                                Value = albumChangeLog
                            });

                            DHAlbum dhAlbum = await _unitOfWork.MusicAPI.CreateAlbum(_appSettings.Value.MasterWSId, dHAlbum);

                            if (dhAlbum!=null)
                            {
                                upload_Album.dh_album_id = dhAlbum.id;
                                upload_Album.modified = false;
                                var albumJson = JsonConvert.DeserializeObject<EditAlbumMetadata>(upload_Album.metadata_json);
                                albumJson.dh_album_id = (Guid)dhAlbum.id;
                                upload_Album.metadata_json = JsonConvert.SerializeObject(albumJson);
                                await _unitOfWork.UploadAlbum.UpdateAlbumByDHAlbumId(upload_Album);

                                _ = Task.Run(() => _unitOfWork.MusicAPI.CopyArtwork((Guid)upload_Album.dh_album_id, artworkUrl)).ConfigureAwait(false);                               
                            }
                            albumUpload = true;
                        }

                        editAlbumMetadata.dh_album_id = upload_Album.dh_album_id;
                        editAlbumMetadata.UploadId = upload_Album.upload_id;
                        editAlbumMetadata.artwork_url = upload_Album.artwork;
                    }

                    dhTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, trackUploadId.ToString());
                    dhTrack.miscellaneous = new DHTMiscellaneous() { sourceRef = enMLUploadAction.ML_TRACK_COPY.ToString() };

                    if (upload_Album?.dh_album_id != null)
                    {
                        dhTrack.albumId = upload_Album.dh_album_id;
                    }
                    else
                    {
                        dhTrack.albumId = null;
                    }

                    dhTrack.libraryId = null;
                    dhTrack.id = copyDHTrackId;

                    TrackChangeLog trackChangeLog = new TrackChangeLog()
                    {
                        Action = enTrackChangeLogAction.COPY.ToString(),
                        UserId = orgUser.user_id,
                        DateCreated = DateTime.Now,
                        UserName = orgUser.first_name != null ? orgUser.first_name + " " + orgUser.last_name : "",
                        RefId = trackUploadId,
                        SourceRefId = ml_Master_Track.track_id
                    };

                    dhTrack.descriptiveExtended = new List<Soundmouse.Messaging.Model.DescriptiveData>();
                    dhTrack.descriptiveExtended.Add(new Soundmouse.Messaging.Model.DescriptiveData()
                    {
                        DateExtracted = DateTime.Now,
                        Source = enDescriptiveExtendedSource.ML_COPY.ToString(),
                        Type = enDescriptiveExtendedType.copy_source_track_id.ToString(),
                        Value = trackChangeLog
                    });

                    DHTrack dHTrack = await _unitOfWork.MusicAPI.CreateDHTrack(_appSettings.Value.MasterWSId, dhTrack);

                    if (dHTrack!=null)
                    {
                        await _unitOfWork.TrackOrg.UpdateTrackOrg(new track_org()
                        {
                            id = mLCopyPayload.trackOrgId,
                            last_edited_by = int.Parse(mLCopyPayload.userId),
                            archive = true
                        });

                        Asset sourceAsset = _trackDoc.Assets?.OrderByDescending(a=>a.Size).FirstOrDefault(); 

                        if(sourceAsset!=null)
                            _ = Task.Run(() => _unitOfWork.MusicAPI.DHAssetCopy((Guid)dHTrack.id, _aWSS3Repositor.GeneratePreSignedURLForMlTrack(sourceAsset.BucketName, sourceAsset.Key, sourceAsset.ServiceUrl))).ConfigureAwait(false);
                      
                        editTrackMetadata = dhTrack.CreateEditTrack(dhTrack.title);
                        editTrackMetadata.artwork_url = artworkUrl;

                        long nextUploadId = await _uploaderLogic.NextUploadSessionId();

                        upload_Track = new upload_track()
                        {
                            asset_uploaded = true,
                            copy_source_track_id = ml_Master_Track.track_id,
                            copy_source_album_id = ml_Master_Track.album_id,
                            copy_source_ws_id = ml_Master_Track.workspace_id,
                            dh_track_id = dHTrack.id,
                            created_by = int.Parse(mLCopyPayload.userId),
                            session_id = (int)upload_Session.id,
                            metadata_json = JsonConvert.SerializeObject(editTrackMetadata, new JsonSerializerSettings()),
                            rec_type = "COPY",
                            date_created = DateTime.Now,
                            date_last_edited = DateTime.Now,
                            search_string = JsonConvert.SerializeObject(editTrackMetadata, new JsonSerializerSettings()),
                            id = (Guid)dHTrack.id,
                            ws_id = Guid.Parse(_appSettings.Value.MasterWSId),
                            performer = editTrackMetadata.performers != null ? String.Join(", ", editTrackMetadata.performers) : string.Empty,
                            track_name = editTrackMetadata.track_title,
                            album_name = dHAlbum?.name,
                            upload_id = trackUploadId,
                            last_edited_by = int.Parse(mLCopyPayload.userId),
                            upload_session_id = nextUploadId
                        };

                        editTrackMetadata.UploadId = upload_Track.upload_id;
                        editTrackMetadata.dhTrackId = upload_Track.dh_track_id;
                        editTrackMetadata.version_id = dHTrack.versionId;
                        editTrackMetadata.id = trackUploadId.ToString();

                        if (upload_Album?.dh_album_id != null)
                        {
                            upload_Track.dh_album_id = upload_Album.dh_album_id;
                            upload_Track.ml_album_id = upload_Album.id;                           
                        }
                        await _unitOfWork.UploadTrack.Save(upload_Track);

                        if (editAlbumMetadata == null || editAlbumMetadata?.dh_album_id != null)
                            await _uploaderLogic.CreateMasterTrackAndAlbumFromUploads(mLCopyPayload.orgId, (Guid)upload_Track.dh_track_id, editTrackMetadata, editAlbumMetadata, orgUser, upload_Track, albumUpload, enUploadRecType.COPY);

                        log_user_action actionLog = new log_user_action
                        {
                            data_type = "track",
                            date_created = DateTime.Now,
                            user_id = Convert.ToInt32(mLCopyPayload.userId),
                            org_id = mLCopyPayload.orgId,
                            data_value = "",
                            action_id = (int)enActionType.COPY_TRACK,
                            ref_id = ml_Master_Track.workspace_id,
                            status = (int)enLogStatus.Success
                        };
                        await _unitOfWork.ActionLogger.Log(actionLog);
                       
                    }

                }
                return editTrackMetadata;
            }
            catch (Exception)
            {
                throw;
            }            
        }
    }
}
