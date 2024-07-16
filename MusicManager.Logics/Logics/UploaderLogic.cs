using Elasticsearch.DataMatching;
using Microsoft.Extensions.Logging;
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
using Newtonsoft.Json.Serialization;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.Logics
{
    public class UploaderLogic : IUploaderLogic
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IAWSS3Repository _aWSS3Repository;
        private readonly IElasticLogic _elasticLogic;
        private readonly ICtagLogic _ctagLogic;
        private readonly IMusicAPIRepository _musicAPIRepository;
        private readonly ILogger<UploaderLogic> _logger;

        public UploaderLogic(IUnitOfWork unitOfWork,
            IOptions<AppSettings> appSettings,
            IAWSS3Repository AWSS3Repository,
            IElasticLogic elasticLogic,
            ICtagLogic ctagLogic,
            IMusicAPIRepository musicAPIRepository,
            ILogger<UploaderLogic> logger)
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;
            _aWSS3Repository = AWSS3Repository;
            _elasticLogic = elasticLogic;
            _ctagLogic = ctagLogic;
            _musicAPIRepository = musicAPIRepository;
            _logger = logger;
        }

        public Product CreateProductFromEditAlbumMetadata(EditAlbumMetadata editAlbumMetadata)
        {
            Product product = new Product()
            {
                Id = (Guid)editAlbumMetadata.dh_album_id,
                ArtworkUri = editAlbumMetadata.artwork_url,
                Name = editAlbumMetadata.album_title,
                Artist = editAlbumMetadata.album_artist,
                Notes = editAlbumMetadata.album_notes,
                NumberOfDiscs = editAlbumMetadata.album_discs.ToString(),
                SubName = editAlbumMetadata.album_subtitle,
                CLine = editAlbumMetadata.cLine,
                Identifiers = new Dictionary<string, string>(),
                VersionId = editAlbumMetadata.version_id
            };
            product.Identifiers.Add("extsysref", editAlbumMetadata.UploadId?.ToString());

            if (!string.IsNullOrEmpty(editAlbumMetadata.upc))
                product.Identifiers.Add("upc", editAlbumMetadata.upc);

            if (!string.IsNullOrEmpty(editAlbumMetadata.catalogue_number))
                product.Identifiers.Add("catalogue_number", editAlbumMetadata.catalogue_number);

            if (!string.IsNullOrEmpty(editAlbumMetadata.album_release_date)
                && DateTime.TryParse(editAlbumMetadata.album_release_date, out DateTime dateTime))
            {
                product.ReleaseDate = dateTime;
            }
            return product;
        }

        public async Task<MLTrackDocument> CreateMasterTrackAndAlbumFromUploads(
            string orgId, 
            Guid dhTrackId,
            EditTrackMetadata editTrackMetadata,
            EditAlbumMetadata editAlbumMetadata,
            org_user orgUser,
            upload_track uploadTrack, 
            bool indexAlbum,
            enUploadRecType enUploadRecType,
            bool isLiveCopy = false
            )
        {

            try
            {
                workspace_org workspace_org = await _unitOfWork.Workspace.GetWorkspaceOrgByOrgId(Guid.Parse(_appSettings.Value.MasterWSId),
                    orgId);

                TrackChangeLog trackChangeLog = new TrackChangeLog();
                Guid? mlVersionId = editTrackMetadata.version_id;
                Track track = new Track()
                {
                    Id = dhTrackId,
                    WorkspaceId = Guid.Parse(_appSettings.Value.MasterWSId),
                    Assets = new Collection<Asset>(),
                    Territories = new Collection<string> { "UK" },
                    TrackData = new TrackData()
                    {
                        InterestedParties = new Collection<InterestedParty>(),
                        Title = editTrackMetadata.track_title,
                        FileName = editTrackMetadata.file_name,
                        Position = editTrackMetadata.position,
                        DiscNumber = editTrackMetadata.disc_number,
                        MusicOrigin = editTrackMetadata.musicorigin,
                        AlternativeTitle = editTrackMetadata.alternate_title,
                        Notes = string.IsNullOrWhiteSpace(editTrackMetadata.track_notes) ? null : editTrackMetadata.track_notes,
                        Duration = editTrackMetadata.duration,
                        Bpm = string.IsNullOrWhiteSpace(editTrackMetadata.bpm) ? null : editTrackMetadata.bpm,
                        Tempo = string.IsNullOrWhiteSpace(editTrackMetadata.tempo) ? null : editTrackMetadata.tempo,
                        PLine = string.IsNullOrWhiteSpace(editTrackMetadata.pLine) ? null : editTrackMetadata.pLine,
                        Genres = editTrackMetadata.genres?.Count() > 0 ? editTrackMetadata.genres : null,
                        Styles = editTrackMetadata.styles?.Count() > 0 ? editTrackMetadata.styles : null,
                        Moods = editTrackMetadata.moods?.Count() > 0 ? editTrackMetadata.moods : null,
                        Instrumentations = editTrackMetadata.instruments?.Count() > 0 ? editTrackMetadata.instruments : null,
                        Keywords = editTrackMetadata.keywords?.Count() > 0 ? editTrackMetadata.keywords : null,
                        Identifiers = new Dictionary<string, string>(),
                        Miscellaneous = new Dictionary<string, string>()
                    }
                };

                track.TrackData.Identifiers.Add("extsysref", editTrackMetadata.UploadId.ToString());
                track.TrackData.Miscellaneous.Add("sourceVersionId", mlVersionId.ToString());

                if (!string.IsNullOrEmpty(editTrackMetadata.isrc))
                    track.TrackData.Identifiers.Add("isrc", editTrackMetadata.isrc);

                if (!string.IsNullOrEmpty(editTrackMetadata.iswc))
                    track.TrackData.Identifiers.Add("iswc", editTrackMetadata.iswc);

                if (!string.IsNullOrEmpty(editTrackMetadata.prs))
                    track.TrackData.Identifiers.Add("prs", editTrackMetadata.prs);

                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.composers, enIPRole.composer);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.publishers, enIPRole.publisher);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.arrangers, enIPRole.arranger);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.performers, enIPRole.performer);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.lyricist, enIPRole.lyricist);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.translators, enIPRole.translator);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.composer_lyricists, enIPRole.composer_lyricist);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.adaptor, enIPRole.adaptor);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_adaptor, enIPRole.sub_adaptor);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_arranger, enIPRole.sub_arranger);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_lyricist, enIPRole.sub_lyricist);
                track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_publisher, enIPRole.sub_publisher);

                if (editTrackMetadata.orgTags?.Count() > 0)
                {
                    track.TrackData.TagsExtended = editTrackMetadata.orgTags.ToArray();
                }

                if (!string.IsNullOrEmpty(editTrackMetadata.rec_label))
                    track.TrackData.InterestedParties.AddInterestedParty(new List<string>() { editTrackMetadata.rec_label }, enIPRole.record_label);

                if (editAlbumMetadata != null)
                {
                    track.TrackData.Product = new Product()
                    {
                        Id = (Guid)editAlbumMetadata.dh_album_id,
                        ArtworkUri = editAlbumMetadata.artwork_url,
                        Name = editAlbumMetadata.album_title,
                        Artist = editAlbumMetadata.album_artist,
                        Notes = editAlbumMetadata.album_notes,
                        NumberOfDiscs = editAlbumMetadata.album_discs?.ToString(),
                        SubName = editAlbumMetadata.album_subtitle,
                        CLine = editAlbumMetadata.cLine,
                        Identifiers = new Dictionary<string, string>(),
                        VersionId = editAlbumMetadata.version_id
                    };

                    track.TrackData.Product.Identifiers.Add("extsysref", editAlbumMetadata.UploadId?.ToString());

                    if (!string.IsNullOrEmpty(editAlbumMetadata.upc))
                        track.TrackData.Product.Identifiers.Add("upc", editAlbumMetadata.upc);

                    if (!string.IsNullOrEmpty(editAlbumMetadata.catalogue_number))
                        track.TrackData.Product.Identifiers.Add("catalogue_number", editAlbumMetadata.catalogue_number);

                    if (!string.IsNullOrEmpty(editAlbumMetadata.album_release_date)
                        && DateTime.TryParse(editAlbumMetadata.album_release_date, out DateTime dateTime))
                    {
                        track.TrackData.Product.ReleaseDate = dateTime;
                    }

                    trackChangeLog = new TrackChangeLog()
                    {
                        Action = enAlbumChangeLogAction.UPLOAD.ToString(),
                        UserId = orgUser.user_id,
                        DateCreated = DateTime.Now,
                        UserName = orgUser.first_name != null ? orgUser.first_name + " " + orgUser.last_name : "",
                        RefId = editAlbumMetadata.UploadId
                    };

                    DescriptiveData descriptiveData = new DescriptiveData()
                    {
                        DateExtracted = DateTime.Now,
                        Source = enDescriptiveExtendedSource.ML_UPLOAD.ToString(),
                        Type = enDescriptiveExtendedType.upload_album_id.ToString(),
                        Value = trackChangeLog
                    };

                    track.TrackData.Product.DescriptiveExtended = new DescriptiveData[1] { descriptiveData };

                    if (editAlbumMetadata.album_orgTags?.Count() > 0)
                    {
                        track.TrackData.Product.TagsExtended = editAlbumMetadata.album_orgTags.ToArray();
                    }

                    if (indexAlbum)
                    {
                        AlbumOrg albumOrg = new AlbumOrg()
                        {
                            id = (Guid)editAlbumMetadata.UploadId,
                            date_created = DateTime.Now,
                            date_last_edited = DateTime.Now
                        };

                        MLAlbumDocument mLAlbumDocument = new MLAlbumDocument();
                        mLAlbumDocument = track.TrackData.Product.GenerateMLAlbumDocument(mLAlbumDocument, null, albumOrg);
                        await _elasticLogic.AlbumIndex(mLAlbumDocument);

                        album_org album_Org = new album_org()
                        {
                            id = (Guid)editAlbumMetadata.UploadId,
                            original_album_id = (Guid)editAlbumMetadata.dh_album_id,                            
                            archive = false,
                            created_by = orgUser.user_id,
                            last_edited_by = orgUser.user_id,
                            source_deleted = false,
                            ml_status = (int)enMLStatus.Live,
                            restricted = false,
                            api_result_id = 0,
                            org_id = orgId                            
                        };

                        if (workspace_org != null)
                            album_Org.org_workspace_id = workspace_org.org_workspace_id;

                        await _unitOfWork.TrackOrg.InsertUpdateAlbumOrg(new List<album_org>() { album_Org }); 
                    }
                }

                if (uploadTrack != null)
                {
                    UploadDesctiptiveRef uploadDesctiptiveRef = new UploadDesctiptiveRef()
                    {
                        Action = enTrackChangeLogAction.UPLOAD.ToString(),
                        UserId = orgUser.user_id,
                        DateCreated = DateTime.Now,
                        UserName = orgUser.first_name != null ? orgUser.first_name + " " + orgUser.last_name : "",
                        RefId = editTrackMetadata.UploadId,
                        AssetS3Id = _appSettings.Value.AWSS3.FolderName + "/" + uploadTrack.s3_id,
                        BucketName = _appSettings.Value.AWSS3.BucketName,
                        Size = uploadTrack.size
                    };

                    DescriptiveData descriptiveData = new DescriptiveData()
                    {
                        DateExtracted = DateTime.Now,
                        Source = enDescriptiveExtendedSource.ML_UPLOAD.ToString(),
                        Type = enDescriptiveExtendedType.upload_track_id.ToString(),
                        Value = uploadDesctiptiveRef
                    };

                    if (!string.IsNullOrEmpty(uploadTrack.s3_id)) {
                        track.Assets.Add(new Asset()
                        {
                            Key = _appSettings.Value.AWSS3.FolderName + "/" + uploadTrack.s3_id,
                            BucketName = _appSettings.Value.AWSS3.BucketName,
                            Type = Path.GetExtension(uploadTrack.s3_id),
                            Quality = 0,
                            Size = -1
                        });
                    }
                   

                    track.TrackData.DescriptiveExtended = new DescriptiveData[1] { descriptiveData };
                }

                MLTrackDocument mLTrackDocument = new MLTrackDocument();
                mLTrackDocument = track.GenerateMLTrackDocument(null, mLTrackDocument, new TrackOrg()
                {
                    org_data = editTrackMetadata.orgTags
                });
                mLTrackDocument.mlCreated = enUploadRecType == enUploadRecType.CREATE ? true : false;
                mLTrackDocument.liveCopy = isLiveCopy;
                mLTrackDocument.id = (Guid)editTrackMetadata.UploadId;
                mLTrackDocument.dhTrackId = dhTrackId;

                await _elasticLogic.TrackIndex(mLTrackDocument);

                await _unitOfWork.TrackAPIResults.Insert(new log_track_api_results()
                {
                    api_call_id = enUploadRecType == enUploadRecType.COPY ? -1 : 0,
                    deleted = false,
                    metadata = JsonConvert.SerializeObject(track, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                    workspace_id = Guid.Parse(_appSettings.Value.MasterWSId),
                    track_id = dhTrackId,
                    received = 0,
                    version_id = mlVersionId,
                    session_id = 0,
                    created_by = Guid.NewGuid(),
                    date_created = DateTime.Now
                });

                List<TrackChangeLog> changeLog = new List<TrackChangeLog>() {
                trackChangeLog
                };

                track_org track_Org = new track_org()
                {
                    id = (Guid)editTrackMetadata.UploadId,
                    original_track_id = dhTrackId,
                    album_id = track?.TrackData?.Product?.Id ?? null,
                    archive = false,
                    created_by = orgUser.user_id,
                    last_edited_by = orgUser.user_id,
                    source_deleted = false,
                    ml_status = (int)enMLStatus.Live,
                    restricted = false,
                    api_result_id = 0,
                    org_id = orgId,
                    change_log = changeLog == null ? null : JsonConvert.SerializeObject(changeLog, new JsonSerializerSettings()),
                    clearance_track = enUploadRecType == enUploadRecType.CREATE ? true : false
                };

                if (workspace_org != null)
                    track_Org.org_workspace_id = workspace_org.org_workspace_id;

                await _unitOfWork.TrackOrg.InsertUpdateTrackOrg(new List<track_org>() { track_Org });

                if (_appSettings.Value.IsLocal == false)
                    await _ctagLogic.UpdatePRSforTrack((Guid)editTrackMetadata.UploadId, mLTrackDocument, null, false);

                return mLTrackDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateMasterTrackAndAlbumFromUploads > " + ex.ToString());
                return null;
            }
        }

        public async Task<MLTrackDocument> UpdateMasterTrackAndAlbumFromUploads(Guid dhTrackId,
             Track track,
             EditTrackMetadata editTrackMetadata,
             EditAlbumMetadata editAlbumMetadata,
             bool isAlbumUpdate, long updatedEpochTime,string prsTunecode
             )
        {
            List<DescriptiveData> albumDescriptiveDatas = new List<DescriptiveData>();           

            List<DescriptiveData> trackDescriptiveDatas = new List<DescriptiveData>();
            trackDescriptiveDatas = track.TrackData.DescriptiveExtended?.ToList();

            Guid? mlVersionId = editTrackMetadata.version_id;

            track.TrackData.Title = editTrackMetadata.track_title.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">"); 
            track.TrackData.FileName = editTrackMetadata.file_name;
            track.TrackData.Position = editTrackMetadata.position;
            track.TrackData.MusicOrigin = editTrackMetadata.musicorigin;
            track.TrackData.AlternativeTitle = editTrackMetadata.alternate_title;
            track.TrackData.Notes = string.IsNullOrWhiteSpace(editTrackMetadata.track_notes) ? null : editTrackMetadata.track_notes;
            track.TrackData.Bpm = string.IsNullOrWhiteSpace(editTrackMetadata.bpm) ? null : editTrackMetadata.bpm;
            track.TrackData.Tempo = string.IsNullOrWhiteSpace(editTrackMetadata.tempo) ? null : editTrackMetadata.tempo;
            track.TrackData.PLine = string.IsNullOrWhiteSpace(editTrackMetadata.pLine) ? null : editTrackMetadata.pLine;
            track.TrackData.Genres = editTrackMetadata.genres?.Count() > 0 ? editTrackMetadata.genres : null;
            track.TrackData.Styles = editTrackMetadata.styles?.Count() > 0 ? editTrackMetadata.styles : null;
            track.TrackData.Moods = editTrackMetadata.moods?.Count() > 0 ? editTrackMetadata.moods : null;
            track.TrackData.Instrumentations = editTrackMetadata.instruments?.Count() > 0 ? editTrackMetadata.instruments : null;
            track.TrackData.Keywords = editTrackMetadata.keywords?.Count() > 0 ? editTrackMetadata.keywords : null;
            track.TrackData.AlternativeTitle = editTrackMetadata.alternate_title;
            track.TrackData.VersionTitle = editTrackMetadata.version_title;
            track.TrackData.Duration = editTrackMetadata.duration;

            if (track.TrackData.Identifiers == null)
                track.TrackData.Identifiers = new Dictionary<string, string>();

            if (track.TrackData.InterestedParties == null)
                track.TrackData.InterestedParties = new Collection<InterestedParty>();

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.composer);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.composers, enIPRole.composer);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.publisher);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.publishers, enIPRole.publisher);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.arranger);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.arrangers, enIPRole.arranger);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.performer);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.performers, enIPRole.performer);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.lyricist);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.lyricist, enIPRole.lyricist);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.composer_lyricist);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.composer_lyricists, enIPRole.composer_lyricist);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.translator);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.translators, enIPRole.translator);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.sub_lyricist);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_lyricist, enIPRole.sub_lyricist);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.adaptor);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.adaptor, enIPRole.adaptor);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.sub_adaptor);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_adaptor, enIPRole.sub_adaptor);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.sub_arranger);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_arranger, enIPRole.sub_arranger);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.original_publisher);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.original_publisher, enIPRole.original_publisher);

            track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.sub_publisher);
            track.TrackData.InterestedParties.AddInterestedParty(editTrackMetadata.sub_publisher, enIPRole.sub_publisher);

            if (!string.IsNullOrEmpty(editTrackMetadata.rec_label))
            {
                track.TrackData.InterestedParties = track.TrackData.InterestedParties.RemoveInterestedParty(enIPRole.record_label);
                track.TrackData.InterestedParties.AddInterestedParty(new List<string>() { editTrackMetadata.rec_label }, enIPRole.record_label);
            }

            track.TrackData.Miscellaneous.UpdateDictionary("sourceVersionId", mlVersionId.ToString());

            track.TrackData.Identifiers.UpdateDictionary("isrc", editTrackMetadata.isrc);
            track.TrackData.Identifiers.UpdateDictionary("iswc", editTrackMetadata.iswc);
            track.TrackData.Identifiers.UpdateDictionary("prs", editTrackMetadata.prs);

            track.TrackData.ContributorsExtended = new Collection<Contributor>();

            if (editTrackMetadata.contributor != null) {
                foreach (var item in editTrackMetadata.contributor)
                {
                    track.TrackData.ContributorsExtended.Add(new Contributor() { Name = item.Name, Role = item.Role });                  
                }
            } 

            if (editTrackMetadata.orgTags?.Count() > 0)
            {
                track.TrackData.TagsExtended = editTrackMetadata.orgTags.ToArray();
            }

            track.TrackData.DiscNumber = editTrackMetadata.disc_number?.ToString();

            if (isAlbumUpdate && editAlbumMetadata != null && track.TrackData.Product != null)
            {
                track.TrackData.Product.Id = (Guid)editAlbumMetadata.dh_album_id;
                track.TrackData.Product.ArtworkUri = editAlbumMetadata.artwork_url;
                track.TrackData.Product.Name = editAlbumMetadata.album_title;
                track.TrackData.Product.Artist = editAlbumMetadata.album_artist;
                track.TrackData.Product.Notes = editAlbumMetadata.album_notes;
                track.TrackData.Product.NumberOfDiscs = editAlbumMetadata.album_discs.ToString();
                track.TrackData.Product.SubName = editAlbumMetadata.album_subtitle;
                track.TrackData.Product.CLine = editAlbumMetadata.cLine;

                if (!string.IsNullOrEmpty(editAlbumMetadata.release_year))
                    track.TrackData.Product.Year = int.Parse(editAlbumMetadata.release_year);

                albumDescriptiveDatas = track.TrackData.Product.DescriptiveExtended?.ToList();

                if (albumDescriptiveDatas == null)
                    albumDescriptiveDatas = new List<DescriptiveData>();

                if (track.TrackData.Product.Identifiers == null)
                    track.TrackData.Product.Identifiers = new Dictionary<string, string>();

                track.TrackData.Product.Identifiers.UpdateDictionary("upc", editAlbumMetadata.upc);
                track.TrackData.Product.Identifiers.UpdateDictionary("catalogue_number", editAlbumMetadata.catalogue_number);

                if (!string.IsNullOrEmpty(editAlbumMetadata.album_release_date))
                    track.TrackData.Product.ReleaseDate = DateTime.Parse(editAlbumMetadata.album_release_date);

                else
                    track.TrackData.Product.ReleaseDate = null;

                if (editAlbumMetadata.album_orgTags?.Count() > 0)
                {
                    track.TrackData.Product.TagsExtended = editAlbumMetadata.album_orgTags.ToArray();
                }

                if (!string.IsNullOrEmpty(editAlbumMetadata.org_album_admin_notes))
                {
                    albumDescriptiveDatas.RemoveAll(a => a.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString());

                    albumDescriptiveDatas.Add(new DescriptiveData()
                    {
                        DateExtracted = DateTime.Now,
                        Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                        Type = enDescriptiveExtendedType.bbc_admin_notes.ToString(),
                        Value = editAlbumMetadata.org_album_admin_notes
                    });
                }

                if (!string.IsNullOrEmpty(editAlbumMetadata.bbc_album_id))
                {
                    albumDescriptiveDatas.RemoveAll(a => a.Type == enDescriptiveExtendedType.bbc_album_id.ToString());

                    albumDescriptiveDatas.Add(new DescriptiveData()
                    {
                        DateExtracted = DateTime.Now,
                        Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                        Type = enDescriptiveExtendedType.bbc_album_id.ToString(),
                        Value = editAlbumMetadata.bbc_album_id
                    });
                }

                track.TrackData.Product.DescriptiveExtended = albumDescriptiveDatas?.ToArray();
            }

            //-- Remove album from track
            if (isAlbumUpdate && editAlbumMetadata == null)
            {
                track.TrackData.Product = null;
            }
            
            if (!string.IsNullOrEmpty(editTrackMetadata.org_admin_notes))
            {
                trackDescriptiveDatas.RemoveAll(a => a.Type == enDescriptiveExtendedType.bbc_admin_notes.ToString());

                trackDescriptiveDatas.Add(new DescriptiveData()
                {
                    DateExtracted = DateTime.Now,
                    Source = enDescriptiveExtendedSource.BBC_FIELDS.ToString(),
                    Type = enDescriptiveExtendedType.bbc_admin_notes.ToString(),
                    Value = editTrackMetadata.org_admin_notes
                });             
            }

            track.TrackData.DescriptiveExtended = trackDescriptiveDatas?.ToArray();

            if (track.Source == null)
                track.Source = new Source();

            if (!string.IsNullOrEmpty(editTrackMetadata.valid_from_date))
            {
                track.Source.ValidFrom = DateTime.Parse(editTrackMetadata.valid_from_date);
            }
            else {
                track.Source.ValidFrom = null;
            }

            if (!string.IsNullOrEmpty(editTrackMetadata.valid_to_date))
            {
                track.Source.ValidTo = DateTime.Parse(editTrackMetadata.valid_to_date);
            }
            else
            {
                track.Source.ValidTo = null;
            }

            await _unitOfWork.TrackAPIResults.Insert(new log_track_api_results()
            {
                api_call_id = 0,
                deleted = false,
                metadata = JsonConvert.SerializeObject(track, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                workspace_id = Guid.Parse(_appSettings.Value.MasterWSId),
                track_id = dhTrackId,
                received = updatedEpochTime,
                version_id = mlVersionId,
                session_id = 0,
                created_by = Guid.NewGuid(),
                date_created = DateTime.Now
            }); 

            MLTrackDocument mLTrackDocument = new MLTrackDocument();
            mLTrackDocument = track.GenerateMLTrackDocument(null, mLTrackDocument, new TrackOrg()
            {
                org_data = editTrackMetadata.orgTags,
                
            });
            mLTrackDocument.id = Guid.Parse(editTrackMetadata.id);

            

            if (_appSettings.Value.IsLocal == false && !string.IsNullOrEmpty(prsTunecode)) {
                editTrackMetadata.prs = prsTunecode;
                mLTrackDocument.prs = prsTunecode;
                await _ctagLogic.UpdatePRSforTrack(editTrackMetadata.UploadId, mLTrackDocument, null, false);
            }               

            return mLTrackDocument;
        }

        public async Task ChangeTrackAlbum(Guid versionId, Track track, Guid uploadId)
        {
            await _unitOfWork.TrackAPIResults.Insert(new log_track_api_results()
            {
                api_call_id = 0,
                deleted = false,
                metadata = JsonConvert.SerializeObject(track, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                workspace_id = Guid.Parse(_appSettings.Value.MasterWSId),
                track_id = track.Id,
                received = 0,
                version_id = versionId,
                session_id = 0,
                created_by = Guid.NewGuid(),
                date_created = DateTime.Now
            });

            MLTrackDocument mLTrackDocument = new MLTrackDocument();
            mLTrackDocument = track.GenerateMLTrackDocument(null, mLTrackDocument, null);
            mLTrackDocument.id = uploadId;


            await _elasticLogic.UpdateIndex(mLTrackDocument);
        }        

        public async Task UpdateDatahubTracksByUploadTracks(List<upload_track> uploadTracks)
        {
            Guid[] ids = uploadTracks.Where(p => p.dh_track_id != null).Select(a => a.id).ToArray();

            IEnumerable<ml_master_track> mlMasterTracks = await _unitOfWork.MLMasterTrack.GetMasterTrackListByIdList(ids);

            foreach (var item in mlMasterTracks)
            {
                if (!item.deleted)
                {
                    Track _trackDoc = JsonConvert.DeserializeObject<Track>(item.metadata);
                    DHTrack dHTrack = CommonHelper.CreateEditAssetHubTrack(_trackDoc, item.ext_sys_ref);
                    dHTrack.position = uploadTracks.SingleOrDefault(p => p.dh_track_id == item.track_id)?.position?.ToString();
                    await _unitOfWork.MusicAPI.UpdateTrack(dHTrack.id.ToString(), dHTrack);
                }
            }

        }

        public async Task UploadArtwork(string key, byte[] content, Guid mlAlbumId)
        {
            try
            {
                string artworkURL = string.Empty;
                if (await _aWSS3Repository.UploadObjectAsync(content, key))
                {
                    artworkURL = _aWSS3Repository.GeneratePreSignedURL(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadArtwork > " + key);
            }
        }

        public async Task CreateUploadTracksOnDatahubAndML(List<UploadTrackT1> uploadTrackT1, string orgId, org_user org_User)
        {
            if (uploadTrackT1.Count > 0)
            {
                Guid? mlAlbumId = null;
                DHAlbum dhAlbum = null;

                foreach (var item in uploadTrackT1)
                {
                    bool albumUpload = false;

                    #region -----------  Create Album ------------------------------------------------------------------------------------


                    if (item.uploadTrack.ml_album_id != null && mlAlbumId != item.uploadTrack.ml_album_id)
                    {
                        upload_album upload_Album = await _unitOfWork.UploadAlbum.GetAlbumById((Guid)item.uploadTrack.ml_album_id);

                        if (upload_Album.dh_album_id == null)
                        {
                            dhAlbum = await _unitOfWork.MusicAPI.PrepareAndCreateAlbum(_appSettings.Value.MasterWSId, upload_Album, org_User);

                            if (dhAlbum != null)
                            {
                                item.uploadTrack.dh_album_id = dhAlbum.id;
                                upload_Album.dh_album_id = dhAlbum.id;
                                upload_Album.modified = false;
                                var albumJson = JsonConvert.DeserializeObject<EditAlbumMetadata>(upload_Album.metadata_json);
                                albumJson.dh_album_id = (Guid)dhAlbum.id;
                                upload_Album.metadata_json = JsonConvert.SerializeObject(albumJson);
                                await _unitOfWork.UploadAlbum.UpdateAlbumByDHAlbumId(upload_Album);

                                item.uploadTrack.modified = false;
                            }
                        }
                        else
                        {
                            item.uploadTrack.dh_album_id = upload_Album.dh_album_id;
                            item.uploadTrack.modified = false;
                            dhAlbum = new DHAlbum() { id = upload_Album.dh_album_id };
                        }

                        await _unitOfWork.UploadTrack.UpdateDHAlbumId(item.uploadTrack);

                        if (item.editAlbumMetadata != null)
                        {
                            item.editAlbumMetadata.dh_album_id = item.uploadTrack.dh_album_id;
                        }

                        albumUpload = true;

                        #region -----------  Upload Artwork ------------------------------------------------------------------------------------
                        try
                        {
                            if (item.uploadTrack.dh_album_id != null && item.artworkData != null)
                            {
                                HttpStatusCode httpStatusCode = await _unitOfWork.MusicAPI.UploadArtwork(upload_Album.dh_album_id.ToString(), item.artworkData);
                                if (httpStatusCode == HttpStatusCode.Created)
                                {
                                    upload_Album.artwork_uploaded = true;
                                    DHAlbum dHAlbum = await _unitOfWork.MusicAPI.GetAlbumById((Guid)upload_Album.dh_album_id);
                                    dhAlbum.versionId = dHAlbum?.versionId;
                                }
                                else
                                {
                                    upload_Album.artwork_uploaded = false;
                                }
                                await _unitOfWork.UploadAlbum.UpdateArtworkUploaded(upload_Album);
                            }
                        }
                        catch (Exception)
                        {

                        }
                        #endregion
                    }

                    #endregion

                    #region -----------  Create Track ------------------------------------------------------------------------------------
                    if (item.uploadTrack.dh_track_id == null)
                    {
                        DHTrack dHTrack = await _unitOfWork.MusicAPI.CreateUploadTrack(_appSettings.Value.MasterWSId, item.uploadTrack, dhAlbum?.id, org_User);

                        if (dHTrack != null)
                        {
                            item.uploadTrack.modified = false;
                            item.uploadTrack.dh_track_id = dHTrack.id;

                            await _unitOfWork.UploadTrack.UpdateDHTrackId(item.uploadTrack);
                        }
                    }

                    #endregion

                    if (item.editAlbumMetadata != null)
                        item.editAlbumMetadata.version_id = dhAlbum?.versionId;

                    await CreateMasterTrackAndAlbumFromUploads(orgId, (Guid)item.uploadTrack.dh_track_id, item.editTrackMetadata, item.editAlbumMetadata, org_User, item.uploadTrack, albumUpload,enUploadRecType.UPLOAD);
                }
            }
        }

        public async Task<int> DeleteTrackBulk(DeleteTrackPayload trackEditDeletePayload)
        {
            IEnumerable<MLTrackDocument> mLTrackDocuments = await _elasticLogic.GetTrackElasticTrackListByIds(trackEditDeletePayload.track_ids.ConvertAll(Guid.Parse).ToArray());

            foreach (var item in mLTrackDocuments)
            {
                await _elasticLogic.DeleteTracks(new Guid[1] { item.id });
            }

            _ = Task.Run(() => RemoveTracksFromDatahub(mLTrackDocuments)).ConfigureAwait(false);

            return await _unitOfWork.UploadTrack.RemoveUploadTracksByUploadIds(trackEditDeletePayload.track_ids.ConvertAll(Guid.Parse).ToArray());
        }

        private async Task RemoveTracksFromDatahub(IEnumerable<MLTrackDocument> mLTrackDocuments)
        {
            foreach (var doc in mLTrackDocuments)
            {
                await _musicAPIRepository.DeleteTrack(doc.dhTrackId.ToString());
            }
        }

        public async Task<UploadObject> ProcessUploaderFiles(TrackXMLPayload trackXMLPayload)
        {
            UploadObject uploadObject = new UploadObject()
            {
                tracks = new List<TrackXMLReturn>()
            };
            List<TrackXMLReturn> trackXMLReturn = new List<TrackXMLReturn>();
            List<UploadTrackT1> uploadTrackT1 = new List<UploadTrackT1>();
            string trackName = null;
            org_user org_User = await _unitOfWork.User.GetUserById(int.Parse(trackXMLPayload.userId));
            if (org_User == null)
                org_User = new org_user() { user_id = int.Parse(trackXMLPayload.userId) };

            long nextUploadId = await NextUploadSessionId();


            foreach (var item in trackXMLPayload.data)
            {
                XMLMetadata xMLMetadata = null;
                BBCXmlMetadata bbcXmlMeta = null;
                // Guid prodId = Guid.NewGuid();               
                byte[] _ArtworkBytes = null;
                EditTrackMetadata editTrackMetadata = null;
                EditAlbumMetadata editAlbumMetadata = null;
                Guid mlVersionId = Guid.NewGuid();

                if (item.xml.Contains("netmix"))
                {
                    bbcXmlMeta = CommonHelper.ExtractBBCXML(item.xml);
                    bbcXmlMeta.FileName = string.Format("{0}.{1}", item.trackName.Replace("&amp;", "&"), item.extention).Replace("&lt;", "<").Replace("&gt;", ">");

                    if (bbcXmlMeta != null)
                        trackName = CommonHelper.GetTrackTitle_BBCXML(bbcXmlMeta);
                }
                else
                {
                    xMLMetadata = CommonHelper.ExtractXML(item.xml);

                    if (xMLMetadata != null)
                        trackName = CommonHelper.GetTrackTitle_XML(xMLMetadata);
                }
                Guid _mlTrackId = Guid.NewGuid();

                upload_track _upload_Track = new upload_track()
                {
                    id = _mlTrackId,
                    modified = false,
                    track_name = string.IsNullOrEmpty(trackName) ? item.trackName.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">") : trackName,
                    status = (int)enUploadTrackStatus.Created,
                    size = item.size,
                    session_id = trackXMLPayload.sessionId,
                    s3_id = trackXMLPayload.sessionId + "_" + _mlTrackId + "." + item.extention,
                    track_type = item.type,
                    created_by = org_User.user_id,
                    last_edited_by = org_User.user_id,
                    ws_id = Guid.Parse(_appSettings.Value.MasterWSId),
                    upload_id = Guid.NewGuid(),
                    rec_type = enUploadRecType.UPLOAD.ToString(),
                    archived = false,
                    dh_synced = false,
                    upload_session_id = nextUploadId,
                    xml_md5_hash = item.xmlHash,

                };


                DHAlbum dHAlbum = null;

                if (xMLMetadata != null || bbcXmlMeta != null)
                {
                    if (xMLMetadata != null)
                    {
                        dHAlbum = CommonHelper.CreateAssetHubAlbum(xMLMetadata, enMLUploadAction.ML_ALBUM_ADD.ToString());
                    }
                    else
                    {
                        dHAlbum = CommonHelper.CreateAssetHubAlbumFromBBC(bbcXmlMeta, enMLUploadAction.ML_ALBUM_ADD.ToString());
                    }

                    if (dHAlbum != null)
                    {
                        editAlbumMetadata = dHAlbum?.CreateEditAlbum();

                        upload_album upload_Album = new upload_album()
                        {
                            album_name = dHAlbum.name,
                            artist = dHAlbum.artist.Contains("'") ? dHAlbum.artist.Replace("'", "") : dHAlbum.artist,
                            catalogue_number = xMLMetadata != null ? xMLMetadata.ConvertedFile.IDTags.Catalog : bbcXmlMeta.CatalogueNo,
                            created_by = org_User.user_id,
                            date_created = DateTime.Now,
                            modified = false,
                            release_date = dHAlbum.releaseDate,
                            session_id = trackXMLPayload.sessionId,
                            id = Guid.NewGuid(),
                            date_last_edited = DateTime.Now,
                            last_edited_by = org_User.user_id,
                            upload_id = Guid.NewGuid(),
                        };

                        upload_album _ExistingAlbum = await _unitOfWork.UploadAlbum.CheckAlbumForUpload(upload_Album);
                        //int _trackCount = 0;

                        //if (_ExistingAlbum != null)
                        //_trackCount = await _unitOfWork.UploadAlbum.GetTrackCountOfCurrentSession(_ExistingAlbum);

                        if (_ExistingAlbum != null) // && _trackCount > 0
                        {
                            EditAlbumMetadata _editAlbumMetadata1 = JsonConvert.DeserializeObject<EditAlbumMetadata>(_ExistingAlbum.metadata_json);

                            if (_editAlbumMetadata1?.version_id != null)
                                editAlbumMetadata.version_id = _editAlbumMetadata1?.version_id;

                            upload_Album = _ExistingAlbum;
                            editAlbumMetadata.dh_album_id = upload_Album.dh_album_id;
                            _upload_Track.ml_album_id = upload_Album.id;
                        }
                        else
                        {
                            editAlbumMetadata.id = upload_Album.id;
                            upload_Album.metadata_json = JsonConvert.SerializeObject(editAlbumMetadata, new JsonSerializerSettings());
                            editAlbumMetadata.version_id = mlVersionId;
                            await _unitOfWork.UploadAlbum.CreateAlbum(upload_Album);

                            _upload_Track.ml_album_id = upload_Album.id;
                        }

                        if (!string.IsNullOrEmpty(item.artwork))
                        {
                            try
                            {
                                string _key = _appSettings.Value.AWSS3.FolderName + "/ARTWORK/" + trackXMLPayload.sessionId + "_" + upload_Album.id + ".jpg";
                                _ArtworkBytes = Convert.FromBase64String(item.artwork.Split(',')[1]);

                                if (await _aWSS3Repository.UploadObjectAsync(_ArtworkBytes, _key))
                                {
                                    upload_Album.artwork = _aWSS3Repository.GeneratePreSignedURL(_key);
                                    upload_Album.artwork_uploaded = true;

                                    await _unitOfWork.UploadAlbum.UpdateArtwork(upload_Album);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Artwork upload failed");
                            }
                        }

                        editAlbumMetadata.UploadId = upload_Album.upload_id;
                        editAlbumMetadata.artwork_url = upload_Album.artwork;

                        DHTrack dHTrack = null;
                        if (xMLMetadata != null)
                        {
                            dHTrack = CommonHelper.CreateAssetHubTrack(xMLMetadata, _mlTrackId.ToString(), upload_Album.id.ToString(), enMLUploadAction.ML_TRACK_ADD.ToString());
                        }
                        else
                        {
                            dHTrack = CommonHelper.CreateAssetHubTrackFromBBC(bbcXmlMeta, _mlTrackId.ToString(), upload_Album.id.ToString(), enMLUploadAction.ML_TRACK_ADD.ToString());
                        }

                        dHTrack.filename = item.trackName.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">") + "." + item.extention;

                        editTrackMetadata = dHTrack.CreateEditTrack(dHTrack.title);
                        editTrackMetadata.id = _mlTrackId.ToString();
                        editTrackMetadata.version_id = mlVersionId;
                        editTrackMetadata.artwork_url = upload_Album.artwork;
                        editTrackMetadata.albumId = upload_Album.id;
                        editTrackMetadata.UploadId = _upload_Track.upload_id;

                        _upload_Track.performer = editTrackMetadata.performers != null && editTrackMetadata.performers.Count > 0 ? String.Join(", ", editTrackMetadata.performers.ToArray()) : "";
                        _upload_Track.album_name = dHAlbum != null ? dHAlbum.name : "";
                        _upload_Track.search_string = string.Format("{0}{1}", JsonConvert.SerializeObject(editTrackMetadata, new JsonSerializerSettings()), upload_Album != null ? JsonConvert.SerializeObject(editAlbumMetadata, new JsonSerializerSettings()) : "");

                        _upload_Track.metadata_json = JsonConvert.SerializeObject(editTrackMetadata, new JsonSerializerSettings());                       
                        _upload_Track.position = editTrackMetadata.position.StringToInteger();
                        _upload_Track.disc_no = editTrackMetadata.disc_number.StringToInteger();
                        _upload_Track.isrc = editTrackMetadata.isrc;
                        _upload_Track.iswc = editTrackMetadata.iswc;
                        _upload_Track.file_name = editTrackMetadata.file_name.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
                    }
                }
                else
                {
                    _upload_Track.file_name = item.trackName.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">") + "." + item.extention;
                }

                if (dHAlbum == null)
                {
                    editTrackMetadata = new EditTrackMetadata()
                    {
                        track_title = item.trackName,
                        id = _mlTrackId.ToString(),
                        source_ref = enMLUploadAction.ML_TRACK_ADD.ToString(),
                        musicorigin = "commercial",
                        file_name = string.Format("{0}.{1}", item.trackName.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">"), item.extention)
                    };

                    DHTrack dHTrack = DHTrackEditExtention.CreateDHTrackFromEditTrackMetadata(null, editTrackMetadata, _mlTrackId.ToString());

                    //_upload_Track.dh_track_metadata = JsonConvert.SerializeObject(dHTrack, new JsonSerializerSettings());
                    _upload_Track.metadata_json = JsonConvert.SerializeObject(editTrackMetadata, new JsonSerializerSettings());

                    editTrackMetadata.UploadId = _upload_Track.upload_id;
                    editTrackMetadata.version_id = mlVersionId;
                }

                await _unitOfWork.UploadTrack.Save(_upload_Track);

                trackXMLReturn.Add(new TrackXMLReturn()
                {
                    exist = false,
                    uploadTrack = _upload_Track,
                    fileName = item.trackName.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
                });

                UploadTrackT1 uploadTrackT11 = new UploadTrackT1()
                {
                    exist = false,
                    uploadTrack = _upload_Track,
                    fileName = item.trackName.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">"),
                    artworkData = _ArtworkBytes,
                    editAlbumMetadata = editAlbumMetadata,
                    editTrackMetadata = editTrackMetadata
                };

                uploadTrackT1.Add(uploadTrackT11);

                _logger.LogInformation("Upload track | TrackObject: {TrackObject}, UserId:{UserId}, Module:{Module}", uploadTrackT11, org_User.user_id, "Upload Track");
            }

            uploadObject.tracks = trackXMLReturn?.OrderBy(p => p.uploadTrack.disc_no).ThenBy(p=>p.uploadTrack.position).ToList();
            uploadObject.S3Token = await _aWSS3Repository.GenerateS3SessionTokenAsync();           

           _ = Task.Run(() => CreateUploadTracksOnDatahubAndML(uploadTrackT1, trackXMLPayload.orgId, org_User)).ConfigureAwait(false);

            return uploadObject;
        }

        public async Task<long> NextUploadSessionId()
        {
            long currentValue = await _unitOfWork.UploadTrack.GetUploadSessionId();  
            return currentValue - 1;
        }

        public async Task<MLTrackDocument> CreateNewTrack(TrackCreatePayload trackCreatePayload)
        {
            MLTrackDocument mLTrackDocument = null;
            DHAlbum dHAlbum = null;            

            Guid _mlTrackId = Guid.NewGuid();
            Guid _mlAlbumId = Guid.NewGuid();
            Guid _dhTrackId = Guid.NewGuid();
            Guid _dhAlbumId = Guid.NewGuid();
            Guid mlVersionId = Guid.NewGuid();
            bool isliveCopy = trackCreatePayload.trackData.musicorigin == enMLMusicOrigin.live.ToString() ? true : false;         
            bool createAlbumIndex = false;
            bool updateUploadTrack = false;
            bool updateUploadAlbum = false;
            upload_album upload_Album = new upload_album();

            if (!string.IsNullOrEmpty(trackCreatePayload.trackData.id))
                _mlTrackId = Guid.Parse(trackCreatePayload.trackData.id);

            org_user orgUser = await _unitOfWork.User.GetUserById(int.Parse(trackCreatePayload.userId));

            long nextUploadId = await NextUploadSessionId();

            if (trackCreatePayload.albumdata != null)
            {
                trackCreatePayload.albumdata.id = _dhAlbumId;
                trackCreatePayload.albumdata.UploadId = _mlAlbumId;
                dHAlbum = trackCreatePayload.albumdata.CreateDHAlbumFromEditAlbumMetadata(_mlAlbumId, orgUser);
                if (dHAlbum != null)
                {
                    upload_Album = new upload_album()
                    {
                        album_name = dHAlbum.name,
                        artist = dHAlbum.artist,
                        catalogue_number = trackCreatePayload.albumdata.catalogue_number,
                        created_by = orgUser.user_id,
                        date_created = DateTime.Now,
                        modified = false,
                        release_date = dHAlbum.releaseDate,
                        session_id = 0,
                        id = _dhAlbumId,
                        date_last_edited = DateTime.Now,
                        last_edited_by = orgUser.user_id,
                        upload_id = _mlAlbumId,
                    };


                    if (!isliveCopy)
                    {
                        dHAlbum = await _unitOfWork.MusicAPI.CreateAlbum(_appSettings.Value.MasterWSId, dHAlbum);

                        if (dHAlbum != null)
                        {
                            trackCreatePayload.albumdata.dh_album_id = dHAlbum.id;
                            upload_Album.dh_album_id = dHAlbum.id;
                            trackCreatePayload.albumdata.version_id = mlVersionId;

                            upload_Album.metadata_json = JsonConvert.SerializeObject(trackCreatePayload.albumdata, new JsonSerializerSettings());

                            updateUploadAlbum = true;
                            createAlbumIndex = true;

                            await _unitOfWork.UploadAlbum.CreateAlbum(upload_Album);
                        }
                    }
                    else {
                        trackCreatePayload.albumdata.dh_album_id = _dhAlbumId;
                        upload_Album.dh_album_id = _dhAlbumId;
                        trackCreatePayload.albumdata.version_id = mlVersionId;

                        upload_Album.metadata_json = JsonConvert.SerializeObject(trackCreatePayload.albumdata, new JsonSerializerSettings());
                    }
                        
                }
                else
                {
                    trackCreatePayload.albumdata = null;
                }
            }

            trackCreatePayload.trackData.UploadId = _mlTrackId;
            trackCreatePayload.trackData.id = _dhTrackId.ToString();
            trackCreatePayload.trackData.version_id = Guid.NewGuid();

            upload_track _upload_Track = new upload_track()
            {
                id = _dhTrackId,
                modified = false,
                track_name = trackCreatePayload.trackData.track_title,
                status = (int)enUploadTrackStatus.Created,                
                session_id = 0,
                created_by = orgUser.user_id,
                last_edited_by = orgUser.user_id,
                ws_id = Guid.Parse(_appSettings.Value.MasterWSId),
                upload_id = _mlTrackId,
                rec_type = enUploadRecType.CREATE.ToString(),
                archived = false,
                dh_synced = false,
                upload_session_id = nextUploadId,                
            };

            if (dHAlbum != null)
            {
                _upload_Track.album_name = dHAlbum.name;
                _upload_Track.dh_album_id = dHAlbum.id;
                _upload_Track.ml_album_id = _mlAlbumId;
                trackCreatePayload.trackData.albumId = dHAlbum.id;
            }

            _upload_Track.performer = trackCreatePayload.trackData.performers != null && trackCreatePayload.trackData.performers.Count > 0 ? String.Join(", ", trackCreatePayload.trackData.performers.ToArray()) : "";
            _upload_Track.album_name = dHAlbum != null ? dHAlbum.name : "";
            _upload_Track.search_string = string.Format("{0}{1}", JsonConvert.SerializeObject(trackCreatePayload.trackData, new JsonSerializerSettings()), trackCreatePayload.albumdata != null ? JsonConvert.SerializeObject(trackCreatePayload.albumdata, new JsonSerializerSettings()) : "");

            _upload_Track.metadata_json = JsonConvert.SerializeObject(trackCreatePayload.trackData, new JsonSerializerSettings());
            _upload_Track.position = trackCreatePayload.trackData.position.StringToInteger();
            _upload_Track.disc_no = trackCreatePayload.trackData.disc_number.StringToInteger();
            _upload_Track.isrc = trackCreatePayload.trackData.isrc;
            _upload_Track.iswc = trackCreatePayload.trackData.iswc;
            _upload_Track.file_name = trackCreatePayload.trackData.file_name;         
                

            DHTrack dHTrack = DHTrackEditExtention.CreateDHTrackFromEditTrackMetadata(null, trackCreatePayload.trackData, _mlTrackId.ToString());

            #region -----------  Create Track ------------------------------------------------------------------------------------
            if (!isliveCopy)
            {
                if (dHTrack != null)
                {
                    dHTrack = await _unitOfWork.MusicAPI.CreateDHTrack(_appSettings.Value.MasterWSId, dHTrack);

                    if (dHTrack != null)
                    {
                        trackCreatePayload.trackData.dhTrackId = dHTrack.id;
                        _upload_Track.modified = false;
                        _upload_Track.dh_track_id = dHTrack.id;

                        updateUploadTrack = true;
                        
                        mLTrackDocument = await CreateMasterTrackAndAlbumFromUploads(trackCreatePayload.orgId, (Guid)_upload_Track.dh_track_id, trackCreatePayload.trackData, trackCreatePayload.albumdata, orgUser, _upload_Track, createAlbumIndex, enUploadRecType.CREATE, isliveCopy);
                    }
                }
            }
            else {
                trackCreatePayload.trackData.dhTrackId = _dhTrackId;                
                _upload_Track.dh_track_id = _dhTrackId;
                mLTrackDocument = await CreateMasterTrackAndAlbumFromUploads(trackCreatePayload.orgId, (Guid)_upload_Track.dh_track_id, trackCreatePayload.trackData, trackCreatePayload.albumdata, orgUser, _upload_Track, createAlbumIndex, enUploadRecType.CREATE, isliveCopy);
            }            
            #endregion

            if(updateUploadTrack)
                await _unitOfWork.UploadTrack.Save(_upload_Track);

            if(updateUploadAlbum)
                await _unitOfWork.UploadAlbum.Save(upload_Album);

            return mLTrackDocument;
        }

        public async Task UpdateAlbumIndex(EditAlbumMetadata editAlbumMetadata,Guid dhProdId)
        {
            MLAlbumDocument mLAlbumDocument = await _elasticLogic.GetElasticAlbumByProdId(dhProdId);

            mLAlbumDocument.prodName = editAlbumMetadata.album_title;
            mLAlbumDocument.prodArtist = editAlbumMetadata.album_artist;
            mLAlbumDocument.prodNotes = editAlbumMetadata.album_notes;
            mLAlbumDocument.prodNumberOfDiscs = editAlbumMetadata.album_discs.ToString();
            mLAlbumDocument.upc = editAlbumMetadata.upc;

            if (!string.IsNullOrEmpty(editAlbumMetadata.album_release_date))
                mLAlbumDocument.prodRelease = DateTime.Parse(editAlbumMetadata.album_release_date);

            mLAlbumDocument.catNo = editAlbumMetadata.catalogue_number;
            mLAlbumDocument.cLine = editAlbumMetadata.cLine;

            if (!string.IsNullOrEmpty(editAlbumMetadata.bbc_album_id))
                mLAlbumDocument.bbcAlbumId = editAlbumMetadata.bbc_album_id;

            if (!string.IsNullOrEmpty(editAlbumMetadata.org_album_admin_notes))
                mLAlbumDocument.adminNotes = editAlbumMetadata.org_album_admin_notes;          

            if (editAlbumMetadata.org_album_adminTags?.Count() > 0)            
                mLAlbumDocument.adminTags = editAlbumMetadata.org_album_adminTags;

            await _elasticLogic.AlbumIndex(mLAlbumDocument);

        }
    }
}


