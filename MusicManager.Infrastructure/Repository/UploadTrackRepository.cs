using Dapper;
using Elasticsearch.DataMatching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using MusicManager.Logics.Extensions;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class UploadTrackRepository : GenericRepository<upload_track>, IUploadTrackRepository
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IMusicAPIRepository _musicAPIRepository;
        private readonly ILogger<UploadTrackRepository> _logger;

        public UploadTrackRepository(MLContext context, IOptions<AppSettings> appSettings,
            IMusicAPIRepository musicAPIRepository,
            ILogger<UploadTrackRepository> logger) : base(context)
        {
            _context = context;
            _appSettings = appSettings;
            _musicAPIRepository = musicAPIRepository;
            _logger = logger;
        }

        public MLContext _context { get; }

        public async Task<bool> CheckAndUpdateWhenEdit(UpdateDHTrackPayload updateDHTrackPayload)
        {
            int check = 0;

            bool isGuid = Guid.TryParse(updateDHTrackPayload.dHTrack.uniqueId, out Guid uniqueId);

            bool dhSynced = false;

            try
            {
                if (isGuid)
                {
                    using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                    {
                        upload_track upload_Track = await c.QueryFirstOrDefaultAsync<upload_track>(string.Format("select * from public.upload_track where dh_track_id='{0}'", updateDHTrackPayload.trackMetadata.dhTrackId));

                        if (upload_Track != null)
                        {
                            dhSynced = upload_Track.dh_synced == null ? false : (bool)upload_Track.dh_synced;
                            if (!updateDHTrackPayload.isAlbumEdit)
                            {
                                EditTrackMetadata trackMetadata = JsonConvert.DeserializeObject<EditTrackMetadata>(upload_Track.metadata_json);
                                updateDHTrackPayload.trackMetadata.artwork_url = trackMetadata.artwork_url;
                            }

                            upload_Track.metadata_json = JsonConvert.SerializeObject(updateDHTrackPayload.trackMetadata, new JsonSerializerSettings());
                            upload_Track.search_string = upload_Track.metadata_json;
                            upload_Track.last_edited_by = updateDHTrackPayload.userId != null ? Convert.ToInt32(updateDHTrackPayload.userId) : 0;

                            if (updateDHTrackPayload.albumMetadata != null)
                                upload_Track.album_name = updateDHTrackPayload.albumMetadata.album_title;

                            upload_Track.track_name = updateDHTrackPayload.trackMetadata.track_title.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">"); 
                            upload_Track.position = updateDHTrackPayload.trackMetadata.position.StringToInteger();
                            upload_Track.iswc = updateDHTrackPayload.trackMetadata.iswc;
                            upload_Track.isrc = updateDHTrackPayload.trackMetadata.isrc;
                            upload_Track.duration = updateDHTrackPayload.trackMetadata.duration;
                            upload_Track.disc_no = updateDHTrackPayload.trackMetadata.disc_number.StringToInteger();


                            upload_Track.performer = updateDHTrackPayload.trackMetadata.performers != null && updateDHTrackPayload.trackMetadata.performers.Count > 0 ? String.Join(", ", updateDHTrackPayload.trackMetadata.performers) : "";
                            
                            if (upload_Track.ml_album_id != null && updateDHTrackPayload.isAlbumEdit)
                            {
                                upload_album upload_Album = new upload_album()
                                {
                                    metadata_json = JsonConvert.SerializeObject(updateDHTrackPayload.albumMetadata, new JsonSerializerSettings()),
                                    album_name = updateDHTrackPayload.albumMetadata.album_title,
                                    artist = updateDHTrackPayload.albumMetadata.album_artist,
                                    artwork = updateDHTrackPayload.albumMetadata.artwork_url,
                                    last_edited_by = updateDHTrackPayload.userId != null ? Convert.ToInt32(updateDHTrackPayload.userId) : 0
                                };

                                upload_Track.search_string = upload_Track.search_string + upload_Album.metadata_json;

                                check = await c.ExecuteAsync(string.Format("update upload_album set date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by, metadata_json=CAST(@metadata_json AS json),album_name=@album_name,artist=@artist,artwork=@artwork  where id='{0}';", upload_Track.ml_album_id), upload_Album);
                            }

                            check = await c.ExecuteAsync(string.Format("update upload_track set date_last_edited=CURRENT_TIMESTAMP,last_edited_by=@last_edited_by, album_name=@album_name, track_name=@track_name, performer=@performer, metadata_json=CAST(@metadata_json AS json),search_string=@search_string,position=@position,iswc=@iswc,isrc=@isrc,disc_no=@disc_no where id=@id;"), upload_Track);

                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return dhSynced;
        }

        public async Task<upload_track> GetTrack(upload_track upload_Track)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("select * from upload_track where ");
                stringBuilder.AppendFormat(" track_name='{0}' and session_id={2}", upload_Track.track_name, upload_Track.session_id);

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryFirstOrDefaultAsync<upload_track>(stringBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<int> UpdateSyncStatus(Guid id, float? duration)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                upload_track uTrack = await GetUploadTrackByDhTrackId(id);
                if (uTrack != null && uTrack.metadata_json != null)
                {
                    var metadata = JsonConvert.DeserializeObject<EditTrackMetadata>(uTrack.metadata_json);
                    metadata.duration = duration;

                    return await c.ExecuteAsync("update public.upload_track set dh_synced = true,metadata_json=CAST(@metadata_json AS json),duration=@duration where dh_track_id=@dh_track_id",
                        new upload_track { dh_track_id = id, metadata_json = JsonConvert.SerializeObject(metadata), duration = duration });
                }
                else {
                    return -2;
                }
            }
        }

        public async Task<bool> UpdateTracksFromAlbumId(Guid trackId, Guid albumId, string dhAlbumId, EditTrackMetadata metadata)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                var result = await c.ExecuteAsync("update public.upload_track set album_name=@album_name, ml_album_id = @ml_album_id, dh_album_id=@dh_album_id, metadata_json=CAST(@metadata_json AS json)  where id = @id",
                    new upload_track { ml_album_id = albumId, dh_album_id = Guid.Parse(dhAlbumId), id = trackId, metadata_json = JsonConvert.SerializeObject(metadata, new JsonSerializerSettings()) });
                if (result == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<bool> UpdateTrackDhAlbumMetaData(upload_track track)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                var result = await c.ExecuteAsync("update public.upload_track set metadata_json=CAST(@metadata_json AS json)  where id = @id",
                    track);
                if (result == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<List<upload_track>> GetTracksFromAlbumId(Guid albumId)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                string query = "select * from upload_track where ml_album_id = @ml_album_id order by disc_no,position";
                var result = await c.QueryAsync<upload_track>(query, new upload_track { ml_album_id = albumId });
                return result.AsList();
            }
        }

        public async Task<bool> RemoveAlbumFromTrack(Guid trackId, EditTrackMetadata metadata)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                string query = "update public.upload_track set ml_album_id=@ml_album_id, dh_album_id=@dh_album_id, metadata_json=CAST(@metadata_json AS json)  where id = @id";
                var result = await c.ExecuteAsync(query, new upload_track { id = trackId, metadata_json = JsonConvert.SerializeObject(metadata, new JsonSerializerSettings()), ml_album_id = null, dh_album_id = null });
                if (result == 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<int> UpdateByHDTrack(DHTrack dHTrack, DHAlbum dHAlbum, string artworkUrl, upload_track uploadTrack, upload_album uploadAlbum)
        {
            if (uploadTrack == null)
                uploadTrack = await GetUploadTrackByDhTrackId((Guid)dHTrack.id);

            if (uploadTrack == null)
                return 0;

            EditTrackMetadata mLTrackMetadataEdit = dHTrack.CreateEditTrack(dHTrack.title);
            EditAlbumMetadata editAlbumMetadata = null;

            if (dHAlbum != null)
            {
                editAlbumMetadata = dHAlbum.CreateEditAlbum();
                editAlbumMetadata.id = uploadAlbum.id;
                editAlbumMetadata.dh_album_id = dHAlbum.id;
                uploadTrack.ml_album_id = uploadAlbum.id;
                uploadTrack.dh_album_id = dHAlbum.id;

                if (string.IsNullOrWhiteSpace(artworkUrl))
                    artworkUrl = await _musicAPIRepository.GetAlbumArtwork((Guid)dHTrack.albumId);

            }

            mLTrackMetadataEdit.artwork_url = artworkUrl;

            uploadTrack.album_name = dHAlbum.name;
            uploadTrack.search_string = JsonConvert.SerializeObject(mLTrackMetadataEdit, new JsonSerializerSettings()) + editAlbumMetadata != null ? JsonConvert.SerializeObject(editAlbumMetadata, new JsonSerializerSettings()) : "";
            uploadTrack.metadata_json = JsonConvert.SerializeObject(mLTrackMetadataEdit, new JsonSerializerSettings());           

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                string sql = @"update  upload_track set album_name=@album_name, search_string=@search_string ,metadata_json=CAST(@metadata_json AS json), 
                ml_album_id = @ml_album_id , dh_album_id = @dh_album_id
                where id = @id";

                return await c.ExecuteAsync(sql, uploadTrack);
            }
        }

        public async Task<upload_track> GetUploadTrackByUploadId(Guid id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<upload_track>("select * from upload_track ut where ut.upload_id = @upload_id or ut.id=@id", new upload_track()
                {
                    upload_id = id,
                    id = id
                });
            }
        }

        public async Task<List<upload_track>> GetTracksByUploadStatus(string status)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                var result = await c.QueryAsync<upload_track>("select * from upload_track ut where ut.asset_upload_status = @asset_upload_status", new upload_track()
                {
                    asset_upload_status = status
                });
                return result.ToList();
            }
        }

        public async Task<upload_track> GetUploadTrackById(Guid id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<upload_track>("select * from upload_track ut where ut.id = @id", new upload_track()
                {
                    id = id
                });
            }
        }

        public async Task<upload_track> GetUploadTrackByDhTrackId(Guid id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QuerySingleOrDefaultAsync<upload_track>("select * from upload_track ut where ut.dh_track_id = @dh_track_id", new upload_track()
                {
                    dh_track_id = id
                });
            }
        }

        public async Task<upload_track> Save(upload_track upload_Track)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    StringBuilder _SB = new StringBuilder();
                    _SB.Append(@"INSERT INTO public.upload_track
(id,session_id, track_name, size, status,performer,album_name, s3_id, dh_track_id, track_type, metadata_json, modified, asset_uploaded, asset_upload_status, asset_upload_begin, asset_upload_last_check, date_created, created_by, date_last_edited, last_edited_by, search_string, dh_album_id, ml_album_id, artwork_uploaded, rec_type, copy_source_track_id, copy_source_album_id, copy_source_ws_id, dh_synced, ws_id,upload_id,position,isrc,iswc,file_name,disc_no,upload_session_id,xml_md5_hash)
VALUES(@id,@session_id, @track_name, @size, @status,@performer,@album_name, @s3_id, @dh_track_id, @track_type,CAST(@metadata_json AS json) , @modified, @asset_uploaded, @asset_upload_status, @asset_upload_begin, @asset_upload_last_check, CURRENT_TIMESTAMP, @created_by, CURRENT_TIMESTAMP, @last_edited_by, @search_string, @dh_album_id, @ml_album_id, @artwork_uploaded, 
@rec_type, @copy_source_track_id, @copy_source_album_id, @copy_source_ws_id, @dh_synced, @ws_id,@upload_id,@position,@isrc,@iswc,@file_name,@disc_no,@upload_session_id, @xml_md5_hash) returning *;"
                                  );
                    return await c.QuerySingleAsync<upload_track>(_SB.ToString(), upload_Track);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> RemoveUploadTrackByUploadId(Guid id)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                //return await c.ExecuteAsync("update upload_track set archived=true where upload_id = @upload_id;", new upload_track() { upload_id = id });
                return await c.ExecuteAsync("delete from upload_track where upload_id = @upload_id or id=@id;", new upload_track() { upload_id = id, id = id });

            }
        }

        public async Task<int> UpdateDHTrackId(upload_track upload_Track)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync("update upload_track set dh_track_id=@dh_track_id, modified=@modified where id = @id;", upload_Track);
            }
        }

        public async Task<int> UpdateDHAlbumId(upload_track upload_Track)
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync("update upload_track set dh_album_id=@dh_album_id, modified=@modified where id = @id;", upload_Track);
            }
        }

        public async Task<int> UpdateUploadTrack(upload_track upload_Track)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("update upload_track set date_last_edited=CURRENT_TIMESTAMP ");

            if (upload_Track.asset_upload_begin != null)
                stringBuilder.Append(", asset_upload_begin=@asset_upload_begin ");

            if (upload_Track.asset_upload_status != null)
                stringBuilder.Append(", asset_upload_status=@asset_upload_status ");

            if (upload_Track.asset_uploaded != null)
                stringBuilder.Append(", asset_uploaded=@asset_uploaded ");

            stringBuilder.Append(" where id = @id;");

            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteAsync(stringBuilder.ToString(), upload_Track);
            }
        }


        public async Task<long> GetUploadSessionId()
        {
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.ExecuteScalarAsync<long>("SELECT MIN(upload_session_id) as upload_session_id FROM upload_track");
            }
        }

        public async Task ReorderUploadTracks(List<upload_track> uploadTracks)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                foreach (var item in uploadTracks)
                {
                    stringBuilder.Append("update upload_track set date_last_edited=CURRENT_TIMESTAMP,position=@position ");
                    stringBuilder.Append(" where id = @id;");
                    await c.ExecuteAsync(stringBuilder.ToString(), item);
                }
            }
        }

        public async Task<int> RemoveUploadTracksByUploadIds(Guid[] ids)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteAsync("delete from upload_track where upload_id = ANY(@ids)", new { ids = ids });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<upload_track>> GetTracksForAssetUpload(int retries = 2)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    var result = await c.QueryAsync<upload_track>(@"select * from upload_track ut where ut.asset_upload_status = @asset_upload_status and 
                dh_track_id is not null;", new upload_track()
                    {
                        asset_upload_status = "S3 Success"
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "GetTracksForAssetUpload | Retry attempt: {Retry} | Module: {Module}", retries, "Track Upload");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    await GetTracksForAssetUpload(retries - 1);
                }
                _logger.LogError(ex, "GetTracksForAssetUpload | Module: {Module}", "Track Upload");
            }
            return null;
        }

        public async Task<int?> GetTrackCountByAlbumId(Guid mlAlbumId)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteScalarAsync<int>("SELECT count(1) FROM upload_track ut where ut.ml_album_id = @ml_album_id", new { ml_album_id = mlAlbumId });
                }
            }
            catch (Exception)
            {
                return null;
            }
            
        }

        public async Task<IEnumerable<Guid>> GetUniqueAlbumIdsByTrackIds(List<string> trackIds)
        {           
            try
            {                     
                var uploadIds = string.Join(", ", trackIds.Select(x => $"'{x}'"));
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    string sql = string.Format(@"select distinct(ut.ml_album_id) from upload_track ut 
                    where ut.upload_id in ({0}) and ut.ml_album_id is not null;", uploadIds);
                    return await c.QueryAsync<Guid>(sql);
                }
            }
            catch (Exception)
            {
                return null;
            }           
        }
    }
}
