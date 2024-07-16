using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class UploadAlbumRepository : GenericRepository<upload_album>, IUploadAlbumRepository
    {
        private readonly IMusicAPIRepository _musicAPIRepository;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<UploadAlbumRepository> _logger;

        public UploadAlbumRepository(MLContext context,
            IMusicAPIRepository musicAPIRepository,
            IOptions<AppSettings> appSettings,
            ILogger<UploadAlbumRepository> logger
            ) : base(context)
        {
            _context = context;
            _musicAPIRepository = musicAPIRepository;
            _appSettings = appSettings;
            _logger = logger;
        }

        public MLContext _context { get; }

        public async Task<int?> Save(upload_album upload_album)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.InsertAsync(upload_album);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }      

        public async Task<upload_album> GetAlbum(upload_album upload_Album)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("select * from upload_album where session_id={0}", upload_Album.session_id);

                if (!string.IsNullOrEmpty(upload_Album.catalogue_number))
                {
                    stringBuilder.AppendFormat(" and catalogue_number='{0}'", upload_Album.catalogue_number);
                }
                else
                {
                    stringBuilder.AppendFormat(" and album_name='{0}' and artist='{1}'", upload_Album.album_name, upload_Album.artist);
                }


                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryFirstOrDefaultAsync<upload_album>(stringBuilder.ToString());                    
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        public async Task<upload_album> GetAlbumById(Guid id)
        {
            try
            {
                string sql = "select * from upload_album where id=@id";
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QuerySingleOrDefaultAsync<upload_album>(sql, new upload_album { id = id });                    
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public async Task<upload_album> CreateAlbum(upload_album upload_album)
        {
            try
            {
                using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {

                    StringBuilder _SB = new StringBuilder();
                    _SB.Append(@"INSERT INTO public.upload_album(id,session_id, dh_album_id, modified, artwork_uploaded, artist, album_name,
                                                            release_date, metadata_json,date_created, created_by, date_last_edited, last_edited_by, catalogue_number, artwork,rec_type, copy_source_album_id, copy_source_ws_id,upload_id)
                                    VALUES(@id,@session_id, @dh_album_id, @modified, @artwork_uploaded, @artist, @album_name,
                                                            @release_date,CAST(@metadata_json AS json),@date_created, @created_by, @date_last_edited, @last_edited_by, @catalogue_number, @artwork,@rec_type, @copy_source_album_id, @copy_source_ws_id,@upload_id) returning *"
                                  );
                    return await c.QuerySingleAsync<upload_album>(_SB.ToString(), upload_album);                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAlbum");
                throw;
            }
        }

        public async Task<int> UpdateAlbum(upload_album upload_album)
        {
            try
            {
                using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {

                    var query = @"update public.upload_album set session_id=@session_id, dh_album_id=@dh_album_id, modified=@modified, artwork_uploaded=@artwork_uploaded, 
                                     artist=@artist, album_name= @album_name,
                                     release_date=@release_date, metadata_json=CAST(@metadata_json AS json),date_created=@date_created, created_by=@created_by, 
                                     date_last_edited=CURRENT_TIMESTAMP, last_edited_by=@last_edited_by, catalogue_number=@catalogue_number, artwork=@artwork,
                                     rec_type=@rec_type, copy_source_album_id=@copy_source_album_id, copy_source_ws_id=@copy_source_ws_id where id=@id";
                    var result = await c.ExecuteAsync(query, upload_album);
                    return result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> UpdateArtworkUploaded(upload_album upload_album)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    var query = @"update public.upload_album set  artwork_uploaded=@artwork_uploaded where id=@id";
                    var result = await c.ExecuteAsync(query, upload_album);
                    return result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> UpdateAlbumByDHAlbumId(upload_album upload_album)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {

                    var query = @"update public.upload_album set  dh_album_id=@dh_album_id, modified=@modified , metadata_json=CAST(@metadata_json as json)
                                      where id=@id";
                    var result = await c.ExecuteAsync(query, upload_album);
                    return result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> DeleteUploadAlbumAndDeleteFromDatahub(upload_album upload_album)
        {
            try
            {
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    await c.ExecuteAsync("delete from upload_album where id = @id or upload_id = @upload_id", upload_album);
                    var albumResponse = await _musicAPIRepository.DeleteAlbum(upload_album.dh_album_id.ToString());                   
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<string> RemoveUploadAlbum(upload_album upload_album)
        {
            string status = string.Empty;
            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                if (upload_album != null)
                {
                    var result = await c.ExecuteAsync("delete from upload_album where id = @id", new upload_album { id = upload_album.id });
                    var album_result = await c.ExecuteAsync("delete from ml_master_album where album_id = @album_id", new album { album_id = upload_album.id });
                    if (result > 0 || album_result > 0)
                    {
                        status = "Success";
                    }
                    else
                    {
                        status = "Failed";
                    }
                }
            }
            return status;
        }


        public async Task<string> RemoveTrackFromAlbum(Guid trackId, enWorkspaceType enWorkspaceType)
        {
            if (enWorkspaceType == enWorkspaceType.External)
                return "External";

            using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
            {
                try
                {
                    var res = await c.ExecuteAsync("delete from upload_track where id = @id", new upload_track { id = trackId });
                    if (res > 0)
                    {
                        return "Success";

                    }
                    else
                    {
                        return "Failed";

                    }

                }
                catch (Exception)
                {
                    throw;
                }
                
            }
        }

        public async Task<string> UploadArtWork(Guid? id, string stream)
        {
            string url = string.Empty;
            if (stream.Contains(","))
            {
                byte[] _ArtworkBytes = Convert.FromBase64String(stream.Split(',')[1]);
                var statusCode = await _musicAPIRepository.UploadArtwork(id.ToString(), _ArtworkBytes);
                if (statusCode == HttpStatusCode.Created)
                {
                    url = await _musicAPIRepository.GetAlbumArtwork(Guid.Parse(id.ToString()));
                    return url;
                }
                else
                {
                    url = string.Empty;
                }
            }
            else
            {
                url = stream;
            }
            return url;
        }

        public async Task<upload_album> GetAlbumByCopySourceId(Guid sourceAlbumId)
        {
            try
            {
                string sql = "select * from upload_album where copy_source_album_id=@copy_source_album_id";
                using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {
                    return await c.QueryFirstOrDefaultAsync<upload_album>(sql, new upload_album { copy_source_album_id = sourceAlbumId });                   
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAlbumByCopySourceId > sourceAlbumId : " + sourceAlbumId);
                return null;
            }
        }

        public async Task<upload_album> GetAlbumByProductId(Guid id)
        {
            string sql = "select * from upload_album where dh_album_id=@dh_album_id";
            using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
            {
                return await c.QueryFirstOrDefaultAsync<upload_album>(sql, new upload_album { dh_album_id = id });
            }
        }

        public async Task<int> UpdateArtwork(upload_album upload_album)
        {
            try
            {
                using (var c = new NpgsqlConnection(_context.Database.GetDbConnection().ConnectionString))
                {

                    var query = @"update public.upload_album set artwork_uploaded=@artwork_uploaded,
                                  date_last_edited=CURRENT_TIMESTAMP, last_edited_by=@last_edited_by, artwork=@artwork
                                  where id=@id";
                    return await c.ExecuteAsync(query, upload_album);                    
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<int> GetTrackCountOfCurrentSession(upload_album upload_Album)
        {
            try
            {
                string sql = "select count(*) from upload_track ut where ut.session_id=@session_id and ut.ml_album_id=@id;";
                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.ExecuteScalarAsync<int>(sql, upload_Album);
                }
            }
            catch (Exception)
            {
                throw;
            }
            
        }

        public async Task<upload_album> CheckAlbumForUpload(upload_album upload_Album)
        {
            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(@"select * from upload_album ua 
                where ua.created_by = @created_by and(ua.session_id in (
                select id from upload_session us
                where us.created_by = @created_by
                order by id limit 3) or ua.date_created > current_date - interval '14 days') ");

                if (!string.IsNullOrEmpty(upload_Album.catalogue_number))
                {
                    stringBuilder.Append(" and catalogue_number=@catalogue_number;");
                }
                else
                {
                    stringBuilder.Append(" and album_name=@album_name and artist=@artist;");
                }

                using (var c = new NpgsqlConnection(_appSettings.Value.NpgConnection))
                {
                    return await c.QueryFirstOrDefaultAsync<upload_album>(stringBuilder.ToString(), upload_Album);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

