using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application;
using MusicManager.Application.Services;
using MusicManager.Core.ExternalApiModels;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Infrastructure.Extentions;
using MusicManager.Infrastructure.Helper;
using MusicManager.Logics.Extensions;
using MusicManager.Logics.Helper;
using MusicManager.Logics.ServiceLogics;
using Nest;
using Newtonsoft.Json;
using Npgsql;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class MLMasterTrackRepository : GenericRepository<ml_master_track>, IMLMasterTrackRepository
    {      
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IUploadTrackRepository _uploadTrackRepository;
        private readonly IMusicAPIRepository _musicAPIRepository;
        private readonly IAWSS3Repository _aWSS3Repositor;
        private readonly ILogger<MLMasterTrackRepository> _logger;
        private readonly IElasticLogic _elasticLogic;

        public MLContext _context { get; }
        public MLMasterTrackRepository(MLContext context, IOptions<AppSettings> appSettings,
            IUploadTrackRepository uploadTrackRepository,
            IMusicAPIRepository musicAPIRepository,
            IAWSS3Repository AWSS3Repositor,
            ILogger<MLMasterTrackRepository> logger,
            IElasticLogic elasticLogic
            ) : base(context)
        {
            _context = context;            
            _appSettings = appSettings;
            _uploadTrackRepository = uploadTrackRepository;
            _musicAPIRepository = musicAPIRepository;
            _aWSS3Repositor = AWSS3Repositor;
            _logger = logger;
            _elasticLogic = elasticLogic;
        }


        public async Task<IEnumerable<MLMasterTrack>> SearchAllFor_CTag_EMI()
        {
            string _sql = string.Format(@"select mmt.track_id,mmt.metadata,to2.c_tags from ml_master_track mmt 
left join track_org to2 on mmt.track_id = to2.id
where to2.c_tags is null and (SELECT string_agg(elem->> 'fullName', ' | ') AS list
FROM json_array_elements((mmt.metadata -> 'trackData'->> 'interestedParties')::json) elem
WHERE lower(elem->> 'role') like any(array['publisher'])) ~* '( |^)emi([^A-z]|$)'");

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<MLMasterTrack>(_sql);
            }
        }


        

        public UpdateDHTrackPayload UpdateEditedTrackObjects(UpdateDHTrackPayload updateDHTrackPayload)
        {
            try
            {
                if (updateDHTrackPayload.isAlbumEdit)
                {
                    updateDHTrackPayload.dHAlbum = updateDHTrackPayload.dHAlbum.UpdateDHAlbumFromEditAlbumMetadata(updateDHTrackPayload.albumMetadata);
                }
                updateDHTrackPayload.dHTrack = DHTrackEditExtention.CreateDHTrackFromEditTrackMetadata(updateDHTrackPayload.dHTrack, updateDHTrackPayload.trackMetadata, updateDHTrackPayload.dHTrack.uniqueId ?? Guid.NewGuid().ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateEditedTrackObjects | {Object}", updateDHTrackPayload);
            }
            return updateDHTrackPayload;
        }

        public async Task<int> UpdateEditeTrackAndAlbumMetadata(ml_master_track ml_Master_Track)
        {
            if (ml_Master_Track.pre_release == null)
            {
                ml_Master_Track.pre_release = false;
            }
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(string.Format("update ml_master_track set pre_release=@pre_release, edit_track_metadata=CAST(@edit_track_metadata AS json),edit_album_metadata=CAST(@edit_album_metadata AS json) where track_id=@track_id;"), ml_Master_Track);
            }
        }


        public async Task<DHTrackEdit> GetTrackForEdit(TrackEditDeletePayload trackEditDeletePayload, string enWorkspaceType, ml_master_track ml_Master_Track, ml_master_album mlMasterAlbum, track_org track_Org, album_org album_Org)
        {
            try
            {
                DHTrackEdit dHTrackEdit = new DHTrackEdit();

                if (ml_Master_Track != null)
                {
                    if (ml_Master_Track != null && ml_Master_Track.metadata != null && !ml_Master_Track.deleted)
                    {
                        Track _trackDoc = JsonConvert.DeserializeObject<Track>(ml_Master_Track.metadata);
                        if (mlMasterAlbum != null) //--- Update Product details from ml_master_album metadata
                            _trackDoc.TrackData.Product = JsonConvert.DeserializeObject<Product>(mlMasterAlbum.metadata);

                        DHTrack dhTrack = dhTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, ml_Master_Track.ext_sys_ref);
                        DHAlbum dHAlbum = CommonHelper.CreateDHAlbumFromProduct(_trackDoc.TrackData.Product, enMLUploadAction.ML_ALBUM_ADD.ToString(), ml_Master_Track.library_id, null);

                        

                        dHTrackEdit.wsId = ml_Master_Track.workspace_id;
                        dHTrackEdit.wsType = enWorkspaceType;

                        if (dhTrack != null)
                        {
                            EditTrackMetadata mLTrackMetadataEdit = null;
                            //var upload_track = await _uploadTrackRepository.GetUploadTrackById(ml_Master_Track.track_id);
                            //if (upload_track == null)
                            //{
                            //    upload_track = await _uploadTrackRepository.GetUploadTrackByUploadId(trackEditDeletePayload.track_id);
                            //}

                            //if (upload_track != null)
                            //{
                            //    mLTrackMetadataEdit = JsonConvert.DeserializeObject<EditTrackMetadata>(upload_track.metadata_json);
                            //}

                            ml_Master_Track.edit_track_metadata = JsonConvert.SerializeObject(dhTrack, new JsonSerializerSettings());
                            if (mLTrackMetadataEdit == null)
                            {
                                mLTrackMetadataEdit = dhTrack.CreateEditTrack(dhTrack.title);
                                mLTrackMetadataEdit.artwork_url = _trackDoc.TrackData.Product != null ? _trackDoc.TrackData.Product.ArtworkUri : null;
                            }
                            mLTrackMetadataEdit.dhTrackId = ml_Master_Track.track_id;
                            dHTrackEdit.trackMetadata = mLTrackMetadataEdit;
                            dHTrackEdit.trackMetadata.id = track_Org.id.ToString();                            

                            if(!string.IsNullOrEmpty(track_Org.c_tags))
                            {
                                ClearanceCTags clearanceCTags = JsonConvert.DeserializeObject<ClearanceCTags>(track_Org.c_tags);
                                dHTrackEdit.trackMetadata.prs_work_publishers = clearanceCTags.workPublishers;
                                dHTrackEdit.trackMetadata.prs_work_title = clearanceCTags.workTitle;
                                dHTrackEdit.trackMetadata.prs_work_tunecode = clearanceCTags.workTunecode;
                                dHTrackEdit.trackMetadata.prs_work_writers = clearanceCTags.workWriters;
                            }

                            if (!string.IsNullOrEmpty(track_Org.org_data))
                            {
                                List<Tag> orgTags = JsonConvert.DeserializeObject<List<Tag>>(track_Org.org_data);

                                Tag subOrigin = orgTags.SingleOrDefault(a=>a.Type == enAdminTypes.SUB_ORIGIN.ToString());

                                if (subOrigin !=null)
                                {
                                    dHTrackEdit.trackMetadata.sub_origin = subOrigin.Value;
                                }
                            }
                        }

                        if (dHAlbum != null)
                        {
                            dHAlbum.id = album_Org?.original_album_id;
                            ml_Master_Track.edit_album_metadata = JsonConvert.SerializeObject(dHAlbum, new JsonSerializerSettings());
                            EditAlbumMetadata editAlbumMetadata = dHAlbum.CreateEditAlbum();
                            editAlbumMetadata.artwork_url = _trackDoc.TrackData.Product != null ? _trackDoc.TrackData.Product.ArtworkUri : null;
                            dHTrackEdit.albumMetadata = editAlbumMetadata;

                            if (album_Org != null) {
                                dHTrackEdit.albumMetadata.dh_album_id = album_Org.original_album_id;
                                dHTrackEdit.albumMetadata.id = album_Org.id;
                            }                          

                            //if (album_Org.org_data != null)
                            //{                               
                            //    dHTrackEdit.albumMetadata.org_album_adminTags = new List<string>();
                            //    dHTrackEdit.albumMetadata.org_album_userTags = new List<string>();
                            //    dHTrackEdit.albumMetadata.album_orgTags = JsonConvert.DeserializeObject<List<Tag>>(album_Org.org_data);
                            //    if (dHTrackEdit.albumMetadata.album_orgTags?.Count > 0)
                            //    {
                            //        if (dHTrackEdit.albumMetadata.album_orgTags.Exists(x => x.Type == enAdminTypes.BBC_ADMIN_NOTES.ToString()))
                            //        {
                            //            dHTrackEdit.albumMetadata.org_album_admin_notes = dHTrackEdit.albumMetadata.album_orgTags.FirstOrDefault(x => x.Type == enAdminTypes.BBC_ADMIN_NOTES.ToString()).Value;
                            //        }                                  
                            //        foreach (var tag in dHTrackEdit.albumMetadata.album_orgTags)
                            //        {
                            //            if (tag.Type == enAdminTypes.BBC_ADMIN_TAG.ToString())
                            //            {
                            //                dHTrackEdit.albumMetadata.org_album_adminTags.Add(tag.Value);
                            //            }
                            //            if (tag.Type == string.Format("{0}_{1}", enAdminTypes.BBC_USER_TAG.ToString(), trackEditDeletePayload.userId))
                            //            {
                            //                dHTrackEdit.albumMetadata.org_album_userTags.Add(tag.Value);
                            //            }
                            //        }
                            //    }
                            //}
                        }

                        dHTrackEdit.dHTrack = dhTrack;
                        dHTrackEdit.dHAlbum = dHAlbum;

                        return dHTrackEdit;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTrackForEdit");
                return null;
            }
        }      

        public async Task CreateTempElasticIndex(MLTrackDocument mLTrackDocument)
        {
            try
            {
                var trackDocument = await _elasticLogic.GetElasticTrackDocById(mLTrackDocument.id); 

                if (trackDocument!=null)
                {
                    mLTrackDocument.assets = trackDocument.assets;
                    mLTrackDocument.dh_synced = trackDocument.dh_synced;
                }
                else
                {
                    mLTrackDocument.dh_synced = false;
                }

                await _elasticLogic.TrackIndex(mLTrackDocument);
            }
            catch (Exception)
            {

            }
        }

        public async Task<ExtTrack> GetElasticTrackById(Guid trackId)
        {
            var extTrack = new ExtTrack();
            try
            {


                var trackDocument = await _elasticLogic.GetElasticTrackDocById(trackId); 

                if (trackDocument !=null)
                {
                    extTrack = new ExtTrack()
                    {
                        albumArtist = trackDocument.prodArtist,
                        albumTitle = trackDocument.prodName,
                        bpm = trackDocument.bpm,
                        duration = trackDocument.duration,
                        albumTags = trackDocument.prodTags,
                        genres = trackDocument.genres,
                        moods = trackDocument.moods,
                        title = trackDocument.trackTitle,
                        tempo = trackDocument.tempo,
                        keywords = trackDocument.keywords,
                        styles = trackDocument.styles,
                        smWorkspaceName = trackDocument.wsName,
                        musicOrigin = trackDocument.musicOrigin,
                        smLibraryName = trackDocument.libName,
                        smTrackId = trackDocument.id,
                        interestedParties = new List<InterestedParties>(),
                        albumIdentifiers = trackDocument.prodIdentifiers,
                        takedownDate = trackDocument.validTo,
                        trackIdentifiers = trackDocument.trackIdentifiers,
                        isTakendown = trackDocument.archived,
                    };

                    if (trackDocument.extIdentifiers?.Count() > 0)
                    {
                        foreach (var item in trackDocument.trackIdentifiers)
                        {
                            if (item.Key == "extsysref")
                                extTrack.extRefTrackId = item.Value;
                        }
                    }

                    if (trackDocument.assets?.Count() > 0)
                    {
                        Soundmouse.Messaging.Model.Asset asset = trackDocument.assets.OrderByDescending(a => a.Size).First();
                        extTrack.audioUrl = _aWSS3Repositor.GeneratePreSignedURLForMlTrack(asset.BucketName, asset.Key, asset.ServiceUrl);
                    }

                    if (trackDocument.ips.Count > 0)
                    {
                        foreach (var item in trackDocument.ips)
                        {
                            InterestedParties interestedParties = new InterestedParties() { role = item.role, name = item.fullName, share = item.societyShare };

                            if (item.identifiers?.Count() > 0)
                            {
                                foreach (var identifier in item.identifiers)
                                {
                                    if (identifier.Key == "ipi")
                                        interestedParties.ipi = identifier.Value;

                                    if (identifier.Key == "isni")
                                        interestedParties.isni = identifier.Value;
                                }
                            }
                            extTrack.interestedParties.Add(interestedParties);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return extTrack;
        }

        public async Task UpdateTrackByMLTrackId(Guid mlTrackid, Guid dhAlbumId, DHAlbum dhAlbum)
        {

            try
            {
                upload_track upload_Track = null;
                ml_master_track ml_Master_Track = null;

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    upload_Track = await c.QuerySingleOrDefaultAsync<upload_track>(string.Format("select * from upload_track ut where ut.id = '{0}';", mlTrackid));

                    if (upload_Track?.dh_track_id != null)
                    {
                        ml_Master_Track = await c.QuerySingleOrDefaultAsync<ml_master_track>(string.Format("select * from ml_master_track mmt where mmt.track_id='{0}'", upload_Track.dh_track_id));
                        if (ml_Master_Track != null)
                        {
                            Track _trackDoc = JsonConvert.DeserializeObject<Track>(ml_Master_Track.metadata);

                            if (_trackDoc != null)
                            {
                                DHTrack dhTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, ml_Master_Track.ext_sys_ref);
                                dhTrack.albumId = dhAlbumId;

                                ml_Master_Track.edit_track_metadata = JsonConvert.SerializeObject(dhTrack, new JsonSerializerSettings());
                                ml_Master_Track.edit_album_metadata = JsonConvert.SerializeObject(dhAlbum, new JsonSerializerSettings());
                                DHTrack check = await _musicAPIRepository.UpdateTrack(upload_Track.dh_track_id.ToString(), dhTrack);
                            }

                            await c.ExecuteAsync(string.Format("update ml_master_track set edit_track_metadata=CAST(@edit_track_metadata AS json), edit_album_metadata=CAST(@edit_album_metadata AS json) where track_id=@track_id;"), ml_Master_Track);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }


        }        

        public async Task<int> RemoveTrack(DeleteTrackPayload trackEditDeletePayload, enWorkspaceType enWorkspaceType)
        {
            if (enWorkspaceType == enWorkspaceType.External)
                return -2;

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                HttpWebResponse httpWebResponse = await _musicAPIRepository.DeleteTrack(trackEditDeletePayload.dhTrackId.ToString());
                if (httpWebResponse?.StatusCode == HttpStatusCode.OK ||
                    httpWebResponse?.StatusCode == HttpStatusCode.NoContent ||
                    httpWebResponse?.StatusCode == HttpStatusCode.NotFound)
                {
                    await c.ExecuteAsync("delete from upload_track where id = @id;", new upload_track() { id = (Guid)trackEditDeletePayload.dhTrackId });
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        public async Task<ml_master_track> GetMaterTrackById(Guid trackId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<ml_master_track>(@"select * 
                                                                            from  ml_master_track mmt                                                                             
                                                                            where mmt.track_id=@track_id;",
                                                                            new ml_master_track()
                                                                            {
                                                                                track_id = trackId,
                                                                            });

            }
        }

        public async Task<ml_master_track> GetMaterTrackByTrackOrgIdOrDhTrackId(track_org track_Org)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<ml_master_track>(@"select * 
                                                                            from  ml_master_track mmt 
                                                                            left join track_org to2 on to2.original_track_id = mmt.track_id 
                                                                            where to2.id=@id or to2.original_track_id=@original_track_id;",
                                                                            track_Org);

            }
        }

        public async Task<ml_master_track> GetMaterTrackByMlId(Guid trackOrgId, string orgId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<ml_master_track>(@"select * 
                                                                            from  ml_master_track mmt 
                                                                            left join track_org to2 on to2.original_track_id = mmt.track_id 
                                                                            where to2.id=@id and to2.org_id=@org_id;",
                                                                            new track_org()
                                                                            {
                                                                                id = trackOrgId,
                                                                                org_id = orgId
                                                                            });

            }
        }

        public async Task<IEnumerable<ml_master_track>> GetMasterTrackListForSetLive(enWorkspaceLib type, Guid refId, long? lastSyncApiResultId, IEnumerable<library_org> library_Orgs, int limit, string orgId, int pageNo)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select * from ml_master_track mmt where 1=1 ");
            ml_master_track ml_Master_Track = new ml_master_track()
            {
                workspace_id = refId,
                library_id = refId,


            };

            if (lastSyncApiResultId !=null )
            {
                stringBuilder.Append(" and mmt.api_result_id > @api_result_id");
                ml_Master_Track.api_result_id = lastSyncApiResultId;
            }

            if (type == enWorkspaceLib.lib)
            {
                stringBuilder.Append(" and mmt.library_id=@library_id");
            }
            else
            {
                stringBuilder.Append(" and mmt.workspace_id=@workspace_id ");

                if (library_Orgs.Count() > 0)
                {
                    stringBuilder.AppendFormat(" and mmt.library_id not in ({0})", string.Join(",", library_Orgs.Select(s => "'" + s.library_id + "'")));
                }
            }

            stringBuilder.AppendFormat(" order by mmt.api_result_id limit {0}", limit);


            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<ml_master_track>(stringBuilder.ToString(), ml_Master_Track);
            }
        }

        public async Task<IEnumerable<ml_master_album>> GetMasterAlbumListForSetLive(enWorkspaceLib type, Guid refId, long? lastSyncApiResultId, IEnumerable<library_org> library_Orgs, int limit)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("select * from ml_master_album mmt where 1=1 ");
            ml_master_album ml_Master_Album = new ml_master_album()
            {
                workspace_id = refId,
                library_id = refId,
            };

            if (lastSyncApiResultId !=null)
            {
                stringBuilder.Append(" and mmt.api_result_id > @api_result_id");
                ml_Master_Album.api_result_id = lastSyncApiResultId;
            }

            if (type == enWorkspaceLib.lib)
            {
                stringBuilder.Append(" and mmt.library_id=@library_id");
            }
            else
            {
                stringBuilder.Append(" and mmt.workspace_id=@workspace_id ");

                if (library_Orgs.Count() > 0)
                {
                    stringBuilder.AppendFormat(" and mmt.library_id not in ({0})", string.Join(",", library_Orgs.Select(s => "'" + s.library_id + "'")));
                }
            }

            stringBuilder.AppendFormat(" order by mmt.api_result_id limit {0}", limit);

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<ml_master_album>(stringBuilder.ToString(), ml_Master_Album);
            }
        }

        public async Task<int> GetMasterTracksCountByAlbumId(Guid albumId, string orgId)
        {
            string sql = @"select count(mmt.track_id) from track_org to2 
left join ml_master_track mmt on to2.original_track_id = mmt.track_id 
where to2.org_id = @org_id and mmt.album_id = @album_id
and to2.source_deleted = false and to2.archive = false  ";

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(sql, new { org_id = orgId, album_id = albumId });
            }
        }

        public async Task<int> GetMasterTracksCountByWorkspaceId(Guid wsId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(@"select count(1) from ml_master_track mmt 
                where mmt.workspace_id = @workspace_id and mmt.deleted=@deleted;", new { workspace_id = wsId, deleted= false });
            }
        }

        public async Task<int> GetMasterTracksCountByLibraryId(Guid LibId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(@"select count(1) from ml_master_track mmt 
                where mmt.library_id = @library_id and mmt.deleted=@deleted;", new { library_id = LibId, deleted = false });
            }
        }

        public async Task<IEnumerable<ml_master_album>> GetMissingMasterAlbumListForSetLive(enWorkspaceLib type, Guid refId, long lastSyncApiResultId, IEnumerable<library_org> library_Orgs, int limit)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(@"select * from ml_master_album mma 
            where mma.workspace_id = '01f534b3-5246-4da9-a8f6-2eb91e99d166'
            and mma.album_id not in (
            select ao.original_album_id from album_org ao
            where ao.org_workspace_id = '9524afb6-763d-44c8-bf06-7dfe0f754b58'
            )");
            ml_master_album ml_Master_Album = new ml_master_album()
            {
                workspace_id = refId,
                library_id = refId,
            };

            if (lastSyncApiResultId > 0)
            {
                stringBuilder.Append(" and mmt.api_result_id > @api_result_id");
                ml_Master_Album.api_result_id = lastSyncApiResultId;
            }

            if (type == enWorkspaceLib.lib)
            {
                stringBuilder.Append(" and mmt.library_id=@library_id");
            }
            else
            {
                stringBuilder.Append(" and mmt.workspace_id=@workspace_id ");

                if (library_Orgs.Count() > 0)
                {
                    stringBuilder.AppendFormat(" and mmt.library_id not in ({0})", string.Join(",", library_Orgs.Select(s => "'" + s.library_id + "'")));
                }
            }

            stringBuilder.AppendFormat(" order by mmt.date_last_edited limit {0}", limit);

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<ml_master_album>(stringBuilder.ToString(), ml_Master_Album);
            }
        }

        public async Task<IEnumerable<ml_master_track>> GetMasterTrackListByIdList(Guid[] ids)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryAsync<ml_master_track>("select * from ml_master_track mmt where mmt.track_id = ANY(@ids)", new { ids = ids });
                }
            }
            catch (Exception)
            {
                throw;
            }            
        }

        public async Task<IEnumerable<ml_master_track>> GetMasterTracksByAlbumId(Guid albumId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryAsync<ml_master_track>("select * from ml_master_track mmt where mmt.album_id = @album_id", new { album_id = albumId });
            }
        }

        public async Task<int> GetTrackUpdatedCountCount(DateTime date)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<int>(@"select count(1) from ml_master_track mmt 
                where mmt.date_last_edited::date = @date_last_edited;", new { date_last_edited = date.Date});
            }
        }
    }
}
