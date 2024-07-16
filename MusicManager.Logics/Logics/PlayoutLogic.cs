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
using MusicManager.Playout.Build.MetadataMapping;
using MusicManager.Playout.Models.Signiant;
using MusicManager.Playout.Models.Signiant.JobRequest;
using Newtonsoft.Json;
using Soundmouse.Messaging.Model;
using Soundmouse.Utils.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;


namespace MusicManager.Logics.Logics
{
    public class PlayoutLogic : IPlayoutLogic
    {

        private readonly IUnitOfWork _unitOfWork;        
        private readonly IOptions<AppSettings> _appSettings;
        private readonly IElasticLogic _elasticLogic;
        private readonly ILogger<PlayoutLogic> _logger;
        private readonly ITrackXMLMapper _trackXmlMapper;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IAWSS3Repository _aWSS3Repositor;
        private readonly IDeliveryDestinationS3ClientRepository _deliveryDestinationS3ClientRepository;
        private readonly IActionLoggerLogic _actionLoggerLogic;

        public PlayoutLogic(IUnitOfWork unitOfWork, IOptions<AppSettings> appSettings, IElasticLogic elasticLogic,
            ILogger<PlayoutLogic> logger, ITrackXMLMapper trackXmlMapper,
            IHttpClientFactory clientFactory,
            IAWSS3Repository AWSS3Repositor,
            IDeliveryDestinationS3ClientRepository deliveryDestinationS3ClientRepository,
            IActionLoggerLogic actionLoggerLogic
            )
        {
            _unitOfWork = unitOfWork;
            _appSettings = appSettings;            
            _elasticLogic = elasticLogic;
            _logger = logger;
            _trackXmlMapper = trackXmlMapper;
            _clientFactory = clientFactory;
            _aWSS3Repositor = AWSS3Repositor;
            _deliveryDestinationS3ClientRepository = deliveryDestinationS3ClientRepository;
            _actionLoggerLogic = actionLoggerLogic;
        }

        

        public async Task<PlayoutResponse> CreatePlayOut(PlayoutPayload playoutPayload, enPlayoutAction action)
        {

            try
            {
                int successCount = 0;
                Guid station_id = Guid.Empty;
                List<PlayoutTrack> trackData = new List<PlayoutTrack>();
                List<PlayoutTrack> trackDataClassic = new List<PlayoutTrack>();
                int sessionId = 0;
                bool multyLocation = false;

                if (!string.IsNullOrEmpty(playoutPayload.station_id))
                {
                    station_id = Guid.Parse(playoutPayload.station_id);
                }

                var station = await _unitOfWork.PlayOut.GetRadioStationById(station_id);

                //Request Queue
                foreach (var publishTrack in playoutPayload.publishTrackData)
                {
                    var track = await _elasticLogic.GetElasticTrackDocByIdForPlayout(Guid.Parse(publishTrack.track_id));
                    if (track != null)
                    {
                        var t = new PlayoutTrack
                        {
                            trackId = (Guid)track.dhTrackId,
                            type = publishTrack.trackType.ToLower(),
                            prsPublishers = track.prsWorkPublishers,
                            mlTrackId = track.id
                        };
                        if (station.delivery_location != station.delivery_location_classical)
                        {
                            multyLocation = true;
                            if (publishTrack.trackType.ToLower() == "classical")
                            {
                                trackDataClassic.Add(t);
                            }
                            else
                            {
                                trackData.Add(t);
                            }
                        }
                        else
                        {
                            trackData.Add(t);
                        }                        
                    }
                }

                if (trackData.Count() > 0)
                {
                    Tuple<int, playout_session> tuplePublishPlayoutResult = await CreateAndPublishPlayout(station, playoutPayload, trackData, action, false, multyLocation);
                    sessionId = tuplePublishPlayoutResult.Item2.id;
                    successCount += tuplePublishPlayoutResult.Item1;
                }

                if (trackDataClassic.Count() > 0)
                {
                    Tuple<int, playout_session> tuplePublishPlayoutResult = await CreateAndPublishPlayout(station, playoutPayload, trackDataClassic, action, true, multyLocation);
                    sessionId = tuplePublishPlayoutResult.Item2.id;
                    successCount += tuplePublishPlayoutResult.Item1;
                }

                return new PlayoutResponse { count = successCount, sessionId = sessionId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreatePlayOut | BuildId: {BuildId} , UserId: {UserId} , Module: {Module}", playoutPayload.build_id, playoutPayload.userId, "Playout");
                throw;
            }

        }

        private async Task<Tuple<int, playout_session>> CreateAndPublishPlayout(
            radio_stations radioStation,
            PlayoutPayload playoutPayload,
            List<PlayoutTrack> trackData,
            enPlayoutAction action,
            bool isClasical,
            bool isMultyLocation
            )
        {
            Guid buildId = Guid.NewGuid();
            int successCount = 0;
            playout_session ps = new playout_session();

            RequestMessage req = new RequestMessage()
            {
                buildId = buildId,
                channels = new List<PlayoutChannel> {
                                new PlayoutChannel
                                {
                                    deliveryLocation = !isClasical ? radioStation.delivery_location : radioStation.delivery_location_classical,
                                    name = radioStation.station
                                }
                            },
                tracks = trackData
            }; 

            if (action == enPlayoutAction.PUBLISH)
            {
                //update playout session
                ps = new playout_session
                {
                    org_id = playoutPayload.org_id,
                    id = playoutPayload.sessionId,
                    station_id = Guid.Parse(playoutPayload.station_id),
                    last_status = (int)enPlayoutStatus.PUBLISHED,
                    request_json = JsonConvert.SerializeObject(req),
                    build_id = buildId
                };
                await _unitOfWork.PlayOut.UpdatePlayoutSessionById(ps);

                foreach (var track in playoutPayload.publishTrackData)
                {
                    await _unitOfWork.PlayOut.UpdateTrackTypeById(int.Parse(track.id), track.trackType, enPlayoutTrackStatus.TO_BE_PUBLISHED);
                }               
            }
            else
            {

                //Create Session
                ps = new playout_session
                {
                    org_id = playoutPayload.org_id,
                    created_by = Convert.ToInt32(playoutPayload.userId),
                    station_id = radioStation.id,
                    last_status = action == enPlayoutAction.CREATE ? (int)enPlayoutStatus.CREATED : (int)enPlayoutStatus.PUBLISHED,
                    track_count = trackData.Count(),
                    request_json = JsonConvert.SerializeObject(req),
                    build_id = buildId,                   
                    publish_status = (int)enPlayoutSessionPublishStatus.PENDING                    
                };

                ps = await _unitOfWork.PlayOut.SavePlayoutSession(ps);

                foreach (var track in playoutPayload.publishTrackData)
                {
                    playout_session_tracks pst = new playout_session_tracks
                    {
                        created_by = Convert.ToInt32(playoutPayload.userId),
                        last_edited_by = Convert.ToInt32(playoutPayload.userId),
                        session_id = ps.id,
                        status = track.asset_status == 0 ? (int)enPlayoutTrackStatus.CREATED :(int)enPlayoutTrackStatus.TO_BE_PUBLISHED,
                        type = playoutPayload.type,
                        track_id = Guid.Parse(track.track_id),
                        track_type = track.trackType != null ? track.trackType : "",
                        title = track.title,
                        isrc = track.isrc != null ? track.isrc : "",
                        performer = track.performer != null ? track.performer : "",
                        artwork_url = track.artwork_url,
                        album_title = track.album_title,
                        dh_track_id = track.dh_track_id,
                        label = track.label,
                        duration = track.duration,
                        asset_status = track.asset_status,
                        xml_status = track.asset_status == 0 ? 0 : (int)enPlayoutTrackXMLStatus.TO_BE_CREATED
                    };

                    if (isMultyLocation)
                    {
                        if (isClasical && track.trackType.ToLower() == "classical")
                        {
                            successCount += await _unitOfWork.PlayOut.SavePlayoutSessionTracks(pst);
                        }
                        else if (!isClasical && track.trackType.ToLower() != "classical")
                        {
                            successCount += await _unitOfWork.PlayOut.SavePlayoutSessionTracks(pst);
                        }
                    }
                    else
                    {
                        successCount += await _unitOfWork.PlayOut.SavePlayoutSessionTracks(pst);
                    }
                }
            }

            //Send to queue
            if (action == enPlayoutAction.CREATE_AND_PUBLISH || action == enPlayoutAction.PUBLISH)
            {          
                _logger.LogInformation("Create playout request |  BuildId: {BuildId} , UserId: {UserId} , Module: {Module}", buildId, playoutPayload.userId, "Playout");
                               
                await _unitOfWork.PlayOut.SavePlayoutResponse(new playout_response() { 
                    build_id = (Guid)ps.build_id,
                    response_time = CommonHelper.GetCurrentUtcEpochTimeMicroseconds(),
                    status = "request_received"                    
                });

                await _unitOfWork.PlayOut.UpdatePlayoutSessionPublishStatus(new playout_session
                {
                    id = ps.id,
                    publish_status = (int)enPlayoutSessionPublishStatus.CREATE
                });
            }
            return Tuple.Create(successCount, ps);
        }       

        private async Task<TranscodeResponse> GetWavFromTranscodeAPI(Guid trackId, int retries = 2)
        {
            var client = _clientFactory.CreateClient();

            try
            {               
                var transcodeApiRequest = new
                    HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.SMCoreApiSettings.API_Endpoint}track-transcode/tracks");

                var requetBody = new { trackId = trackId, parameters = new { format = "WAV", sampleRate = 48000, bitDepth = "PCM16" } };

                transcodeApiRequest.Content = new StringContent(JsonConvert.SerializeObject(requetBody), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(transcodeApiRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TranscodeResponse>(responseString);
                }
                else
                {
                    _logger.LogError("Transcode API error status code | TrackId: {TrackId} , ErrorCode: {ErrorCode} , Module: {Module}", trackId, response.StatusCode, "Playout - Transcode API");
                }
            }
            catch (Exception ex)
            {
                if (retries > 1)
                {
                    _logger.LogWarning(ex, "Transcode API failed | TrackId: {TrackId} | Retry attempt: {Retry} | Module: {Module}", trackId, retries, "Playout - Transcode API");
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    await GetWavFromTranscodeAPI(trackId, retries - 1);
                }
                _logger.LogError(ex, "Transcode API failed | TrackId: {TrackId} , Module: {Module}", trackId, "Playout - Transcode API");
            }
            finally {
                client.Dispose();                
            }
            return null;
        }

        private async Task<int> AddTracksWithValidationCheck(AddPlayoutPayload playOutSession, 
            MLTrackDocument track,
            bool assetFound)
        {
            int successCount = 0;
            playout_session_tracks pst = new playout_session_tracks
            {
                created_by = Convert.ToInt32(playOutSession.userId),
                last_edited_by = Convert.ToInt32(playOutSession.userId),
                session_id = playOutSession.sessionId,
                status = (int)enPlayoutTrackStatus.CREATED,
                type = 1,
                track_id = track.id,
                track_type = playOutSession.type,
                title = track.trackTitle,
                isrc = track.isrc == null ? "" : track.isrc,
                performer = track.performer != null ? String.Join(", ", track.performer.ToArray()) : "",
                artwork_url = track.prodArtworkUrl,
                album_title = track.prodName,
                dh_track_id = track.dhTrackId.ToString(),
                label = track.recordLabel != null ? String.Join(", ", track.recordLabel.ToArray()) : "",
                duration = track.duration,
                asset_status = assetFound ? (int)enPlayoutTrackAssetStatus.TO_BE_DOWNLOADED : (int)enPlayoutTrackAssetStatus.ASSET_NOT_FOUND,
                xml_status = (int)enPlayoutTrackXMLStatus.TO_BE_CREATED
            };

            if (await _unitOfWork.PlayOut.SavePlayoutSessionTracks(pst) > 0) {
                successCount++;
                _logger.LogInformation("Add tracks to Playout |  SessionId: {SessionId} , TrackId: {TrackId} , Module: {Module}", playOutSession.sessionId, track.id, "Playout");
            }            
            return successCount;
        }

        public async Task<int> AddTracksToPlayOut(AddPlayoutPayload playOutSession)
        {
            int successCount = 0;
            var tracks = await _elasticLogic.GetTrackElasticTrackListByIds(playOutSession.selectedTracks.ConvertAll(Guid.Parse).ToArray());
            var existingTracks = await _unitOfWork.PlayOut.GetPlayoutTracksBySessionId(playOutSession.sessionId);

            if (tracks != null && tracks.Count() > 0)
            {
                float totalduration = 0.0f;
                //Check Total Duration
                foreach (var track in tracks)
                {
                    var singleTrackDuration = track.duration != null ? (float)track.duration : 0.0f;
                    totalduration = totalduration += singleTrackDuration;
                }

                var existingTrackTotalDuration = existingTracks.Sum(x => x.duration);

                //Check Total Tracks Duration for 10hours
                if ((existingTracks.Count() + tracks.Count()) <= 50)
                {
                    if ((totalduration + existingTrackTotalDuration) <= 36000)
                    {
                        foreach (var track in tracks)
                        {
                            //Check single Track Duration for 3hours
                            if (track.duration == null || (track.duration != null && track.duration <= 10800))
                            {
                                successCount += await AddTracksWithValidationCheck(playOutSession, track, true);
                            }
                            else
                            {
                                successCount += (int)enPlayoutErrorCodes.THREE_HOURS_SINGLE;
                            }
                        }
                    }
                    else
                    {
                        successCount = (int)enPlayoutErrorCodes.TEN_HOURS_ALL;
                    }
                }
                else
                {
                    successCount = (int)enPlayoutErrorCodes.MAX_TRACK_COUNT_EXEED;
                }
            }
            return successCount;
        }

        public async Task<int> DeletePlayOutTracks(PlayoutTrackIdPayload tracks)
        {
            int deleteCount = 0;
            foreach (var trackId in tracks.trackIds)
            {
                if (!string.IsNullOrEmpty(trackId)) {
                    deleteCount = await _unitOfWork.PlayOut.DeletePlayoutTracks(Convert.ToInt64(trackId));
                    _logger.LogInformation("DeletePlayOutTracks |  TrackId: {TrackId} , UserId: {UserId} , Module: {Module}", trackId, tracks.userId, "Playout");
                }                    
            }
            return deleteCount;
        }
        private async Task<(long totalFileSize, List<Guid> errorTracks)> UploadFilesToPlayoutS3(PlayoutDelivery request,
            playout_session playoutSession)
        {
            long totalFileSize = 0;
            var errorTracks = new List<Guid>();

            foreach (var trackFile in request.trackFiles)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                long xmlLength = 0;
                long assetLength = 0;
                bool uploadSucess = false;
                Stream assetStream = null;
                if (!string.IsNullOrEmpty(trackFile.wav))
                {
                    if (!string.IsNullOrEmpty(trackFile.signedWav))
                        assetStream = await DownloadAssetFile(trackFile.signedWav, trackFile.trackId);

                    if (assetStream == null)
                    {
                        var trackAsset =
                             PreSignedUrlProvider.ExtractBucketAndKeyFromAssetHubUrl(trackFile.wav);

                        string PreSignedURL = _aWSS3Repositor.GeneratePreSignedURLForMlTrack(trackAsset.bucket, trackAsset.key, _appSettings.Value.AWSS3_ASSET_HUB.ServiceUrl, true);

                        if (!string.IsNullOrEmpty(PreSignedURL))
                        {
                            assetStream = await DownloadAssetFile(PreSignedURL, trackFile.trackId);
                        }
                        else
                        {
                            _logger.LogError("PreSignedUrl not found | BuildId: {BuildId} , TrackId: {TrackId} , Module: {Module}", request.id, trackFile.trackId, "Playout");
                        }
                    }
                }

                if (assetStream != null)
                {
                    await _unitOfWork.PlayOut.UpdateTrackAssetStatus(new playout_session_tracks() { id = trackFile.id, asset_status = (int)enPlayoutTrackAssetStatus.DOWNLOADED });

                    xmlLength = trackFile.xmlStream.Length;
                    assetLength = assetStream.Length;
                }
                else
                {
                    _logger.LogError("Asset download failed | BuildId: {BuildId} , TrackId: {TrackId} , Module: {Module}", request.id, trackFile.trackId, "Playout");
                    await _unitOfWork.PlayOut.UpdateTrackAssetStatus(new playout_session_tracks() { id = trackFile.id, asset_status = (int)enPlayoutTrackAssetStatus.DOWNLOAD_FAILED });
                }

                if (trackFile.xmlStream != null && assetStream != null)
                {
                    string xmlPath = $"{request.id}/{trackFile.trackId}.xml";
                    string assetPath = $"{request.id}/{trackFile.trackId}.wav";

                    //-- Change save path for Radio 4 and Radio 4 Extra - BBC CR (ML20-3055)
                    //if (playoutSession.station_id == new Guid("80d445eb-2143-4014-bde2-f59aebc7c4b5"))
                    //{
                    //    xmlPath = $"{request.id}/{trackFile.trackId}.xml_FromMusicBox";
                    //    assetPath = $"{request.id}/{trackFile.trackId}.wav_FromMusicBox";
                    //}
                    //------------------------------------------------------------------------------

                    bool xmlUploadStatus = await _deliveryDestinationS3ClientRepository.UploadFile(xmlPath, trackFile.xmlStream);
                    bool assetUploadStatus = await _deliveryDestinationS3ClientRepository.UploadFile(assetPath, assetStream);

                    await _unitOfWork.PlayOut.UpdateTrackAssetStatus(new playout_session_tracks() { id = trackFile.id, asset_status = assetUploadStatus ? (int)enPlayoutTrackAssetStatus.UPLOADED : (int)enPlayoutTrackAssetStatus.UPLOAD_FAILED });
                    await _unitOfWork.PlayOut.UpdateTrackXmlStatus(new playout_session_tracks() { id = trackFile.id, xml_status = xmlUploadStatus ? (int)enPlayoutTrackAssetStatus.UPLOADED : (int)enPlayoutTrackAssetStatus.UPLOAD_FAILED });

                    if (!xmlUploadStatus)
                    {
                        _logger.LogError("XML upload failed | BuildId: {BuildId} , TrackId: {TrackId} , Module: {Module}", request.id, trackFile.trackId, "Playout");
                    }

                    if (!assetUploadStatus)
                    {
                        _logger.LogError("Asset upload failed | BuildId: {BuildId} , TrackId: {TrackId} , Module: {Module}", request.id, trackFile.trackId, "Playout");
                    }

                    if (xmlUploadStatus && assetUploadStatus)
                    {
                        uploadSucess = true;
                        totalFileSize += xmlLength;
                        totalFileSize += assetLength;
                    }
                }

                if (uploadSucess)
                {
                    await _unitOfWork.PlayOut.UpdateTrackStatus(new playout_session_tracks() { id = trackFile.id, status = (int)enPlayoutTrackStatus.PUBLISHED });
                }
                else
                {
                    await _unitOfWork.PlayOut.UpdateTrackStatus(new playout_session_tracks() { id = trackFile.id, status = (int)enPlayoutTrackStatus.PUBLISH_FAILED });
                    errorTracks.Add(trackFile.trackId);
                }
            }
            return (totalFileSize, errorTracks);
        }

        private async Task<Stream> DownloadAssetFile(string url,Guid trackId)
        {
            Stream stream = null;
            try
            {
                var client = _clientFactory.CreateClient();
                var result = await client.GetAsync(url);
                if (result.IsSuccessStatusCode)
                {
                    stream = await result.Content.ReadAsStreamAsync();
                }
                else {
                    _logger.LogWarning("Download Asset error status code | TrackId: {TrackId} , Module: {Module}", trackId, "Playout - Download Asset");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download Asset failed | TrackId: {TrackId} , Module: {Module}", trackId, "Playout - Download Asset");
            }
            return stream;
        }   

        private JobRequest CreateTransferJob(PlayoutDelivery request,
            string requestId, long totalFileSize, playout_session playoutSession)
        {
            JobRequest job = new JobRequest()
            {
                TransferJob = new TransferJob()
                {
                    BmsBmObjects = new BmsBmObjects() {
                        BmsBmObject = new List<BmsBmObject>()
                    }
                }
            };

            BmsBmObject bmsBmObject = new BmsBmObject()
            {
                BmsBmContents = new BmsBmContents() { BmsBmContent = new List<BmsBmContent>() }
            };

            BmsBmContent bmsBmContent = new BmsBmContent()
            {
                BmsBmContentFormats = new BmsBmContentFormats() { BmsBmContentFormat = new List<BmsBmContentFormat>()}
            };

            BmsBmContentFormat format = new BmsBmContentFormat()
            {
                BmsBmEssenceLocators = new BmsBmEssenceLocators()
                {
                    BmsBmEssenceLocator = new List<BmsBmEssenceLocator>()
                    {
                        new BmsBmEssenceLocator
                        {
                            XsiType
                                = "bms.SimpleFileLocatorType",
                            BmsFile
                                = _appSettings.Value.SigniantConfigs.SimpleFileLocatorPath
                        }
                    }
                }
            };
            format.BmsPackageSize = totalFileSize;

            bmsBmContent.BmsBmContentFormats.BmsBmContentFormat.Add(format);

            bmsBmObject.BmsBmContents.BmsBmContent.Add(bmsBmContent);

            job.TransferJob.BmsBmObjects.BmsBmObject.Add(bmsBmObject);
            job.TransferJob.BmsPriority = "low";
            job.TransferJob.Profiles = new Profiles() { transferProfile = new List<TransferProfile>() };

            TransferProfile profile = new TransferProfile()
            {
                BmsLocation =
                    $"{_appSettings.Value.SigniantConfigs.ServiceBaseUrl}/template/{_appSettings.Value.SigniantConfigs.TransferTemplate}/component/Flight_Download",
                BmsExtensionGroup = new BmsExtensionGroup(),
                transferAtom = new List<TransferAtom>
                {
                    new TransferAtom(){ BmsDestination = _appSettings.Value.SigniantConfigs.SimpleFileLocatorPath}                
                }                
            };
            profile.BmsExtensionGroup.SigSigniantExtensionGroup = new List<SigSigniantExtensionGroup>();

            SigSigniantExtensionGroup sigSigniantExtensionGroup = new SigSigniantExtensionGroup()
            {
                SigJobContextParameters =
                    new SigJobContextParameters
                    {
                        SigJobGroup = _appSettings.Value.SigniantConfigs.JobGroup,
                        SigJobName = $"Soundmouse_{requestId}"
                    },
                SigJobVariables = new List<SigJobVariable>()
            };

            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.sourceOptions.sourceData",
                SigJobVariableValue = string.Join(',',
                      request.trackFiles.Select(t => $"{request.id}/{t.trackId}.wav,{request.id}/{t.trackId}.xml"))
            });


            ////-- Change save path for Radio 4 and Radio 4 Extra - BBC CR (ML20-3055)
            //if (playoutSession.station_id == new Guid("80d445eb-2143-4014-bde2-f59aebc7c4b5"))
            //{
            //    sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            //    {
            //        SigJobVariableName = "Flight_Download.sourceOptions.sourceData",
            //        SigJobVariableValue = string.Join(',',
            //       request.trackFiles.Select(t => $"{request.id}/{t.trackId}.wav_FromMusicBox,{request.id}/{t.trackId}.xml_FromMusicBox"))
            //    });               
            //}                   

           
            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.sourceOptions.flightStorageConfigID",
                SigJobVariableValue = _appSettings.Value.SigniantConfigs.FlightStorageConfigId
            });
            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.targetOptions.targetAgent",
                SigJobVariableValue = _appSettings.Value.SigniantConfigs.TargetAgent
            });
            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.targetOptions.targetFolder",
                SigJobVariableValue = request.deliveryLocation
            });


            profile.BmsExtensionGroup.SigSigniantExtensionGroup.Add(sigSigniantExtensionGroup);
            job.TransferJob.Profiles.transferProfile.Add(profile);

            return job;
        }
        public async Task<byte[]> DownloadPlayoutXML_ZIP(PlayoutXMlDownloadPayload playoutXMlDownloadPayload)
        {
            List<InMemoryFile> inMemoryFiles = new List<InMemoryFile>();

            if (playoutXMlDownloadPayload?.playoutId > 0)
            {
                playoutXMlDownloadPayload.tracks = new List<PlayoutDownloadTrack>();
                var playoutTracks = await _unitOfWork.PlayOut.GetPlayoutTracksBySessionId(playoutXMlDownloadPayload.playoutId);
                foreach (var track in playoutTracks)
                {
                    playoutXMlDownloadPayload.tracks.Add(new PlayoutDownloadTrack()
                    {
                        dhTrackId = Guid.Parse(track.dh_track_id),
                        trackId = track.track_id,
                        trackType = track.track_type
                    });
                }
            }

            int i = 1;
            foreach (var item in playoutXMlDownloadPayload?.tracks)
            {
                try
                {
                    InMemoryFile inMemoryFile = await CreatePlayoutXMLForTrackId(item,i++);

                    if (inMemoryFile != null)
                        inMemoryFiles.Add(inMemoryFile);                   
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DownloadPlayoutXML Error, track id {trackId}", item.dhTrackId);
                }
            }
            return GetZipArchive(inMemoryFiles);
        }

        private async Task<InMemoryFile> CreatePlayoutXMLForTrackId(PlayoutDownloadTrack playoutDownloadTrack,int id)
        {
            ml_master_track mlMasterTrack = await _unitOfWork.MLMasterTrack.GetMaterTrackById(playoutDownloadTrack.dhTrackId);

            EXPORT eXPORT = await CreateEXPORT(playoutDownloadTrack.dhTrackId, playoutDownloadTrack.trackId, playoutDownloadTrack.trackType, mlMasterTrack);

            if (eXPORT == null)
                return null;

            MLTrackDocument mLTrackDocument = await _elasticLogic.GetElasticTrackDocByDhTrackId(playoutDownloadTrack.dhTrackId);

            var memStream = new MemoryStream();

            var xmlWriter = new StreamWriter(memStream, Encoding.UTF8);
            using var writer = new SanitisingXmlWriter(XmlWriter.Create(xmlWriter, new XmlWriterSettings { Indent = true }));
            new XmlSerializer(typeof(EXPORT)).Serialize(writer, eXPORT);

            string fileName = id > 0 ? $"{id}-{mLTrackDocument.CreateFileNameByMLMasterTrack()}.xml" : $"{mLTrackDocument.CreateFileNameByMLMasterTrack()}.xml";

            return new InMemoryFile() { Content = memStream.GetBytes(), FileName = fileName };
        }

        public async Task<InMemoryFile> DownloadPlayoutXML(PlayoutXMlDownloadPayload playoutXMlDownloadPayload)
        {
            if (playoutXMlDownloadPayload.tracks?.Count > 0)
                return await CreatePlayoutXMLForTrackId(playoutXMlDownloadPayload.tracks[0], 0);

            return null;
        }

        private async Task<string> GetPlayoutXMLString(Guid dhTrackId, Guid mlTrackId, string trackType)
        {
            ml_master_track mlMasterTrack = await _unitOfWork.MLMasterTrack.GetMaterTrackById(dhTrackId);

            EXPORT eXPORT = await CreateEXPORT(dhTrackId, mlTrackId, trackType, mlMasterTrack);

            if (eXPORT == null)
                return null;
            
            var xmlWriter = new StringWriter();
            using var writer = new SanitisingXmlWriter(XmlWriter.Create(xmlWriter, new XmlWriterSettings { Indent = true }));
            new XmlSerializer(typeof(EXPORT)).Serialize(writer, eXPORT);
            return xmlWriter.ToString();
        }       

        private async Task<EXPORT> CreateEXPORT(Guid dhTrackId,Guid mlTrackId,string trackType, ml_master_track mlMasterTrack)
        {
            try
            {
                if (mlMasterTrack == null || mlMasterTrack.deleted == true)
                {
                    _logger.LogWarning("DownloadPlayoutXML : Track not found, track id {trackId}", dhTrackId);
                    return null;
                }

                Track _trackDoc = JsonConvert.DeserializeObject<Track>(mlMasterTrack.metadata);
                var mlTrack = await _elasticLogic.GetElasticTrackDocByIdForPlayout(mlTrackId);

                return _trackXmlMapper.MapTrackToFile(_trackDoc, trackType, mlTrack?.prsWorkPublishers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create EXPORT, Track Id {trackId}", dhTrackId);
                return null;
            }           
        }

        private byte[] GetZipArchive(List<InMemoryFile> files)
        {
            byte[] archiveFile;
            using (var archiveStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                {
                    foreach (var file in files)
                    {
                        var zipArchiveEntry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);

                        using var zipStream = zipArchiveEntry.Open();
                        zipStream.Write(file.Content, 0, file.Content.Length);
                    }
                }

                archiveFile = archiveStream.ToArray();
            }

            return archiveFile;
        }

        public async Task ProcessPublishPlayOut()
        {
            //-- Get all playouts of the create and republish status 
            var playouts = await _unitOfWork.PlayOut.GetPlayoutSessionsForPublish();

            //-- Update service log elastic index
            await _actionLoggerLogic.ServiceLog(new ServiceLog()
            {
                id = (int)enServiceType.Playout_service,
                status = enServiceStatus.pass.ToString(),
                serviceName = enServiceType.Playout_service.ToString(),
                timestamp = DateTime.Now
            });

            foreach (var item in playouts)
            {
                //-- Update service log elastic index
                await _actionLoggerLogic.ServiceLog(new ServiceLog()
                {
                    id = (int)enServiceType.Playout_service,
                    status = enServiceStatus.pass.ToString(),
                    serviceName = enServiceType.Playout_service.ToString(),
                    timestamp = DateTime.Now
                });

                //-- Send playout response message
                await _unitOfWork.PlayOut.SavePlayoutResponse(new playout_response()
                {
                    build_id = (Guid)item.build_id,
                    response_time = CommonHelper.GetCurrentUtcEpochTimeMicroseconds(),
                    status = "building_in_progress"
                });

                //-- Update playout session publish status
                await _unitOfWork.PlayOut.UpdatePublishStatus(new playout_session()
                {
                    id = item.id,
                    publish_status = (int)enPlayoutSessionPublishStatus.INPROGRESS
                });

                //-- Update Publish start time
                await _unitOfWork.PlayOut.UpdatePublishStartTime(item.id);

                radio_stations radioStation = await _unitOfWork.PlayOut.GetRadioStationById((Guid)item.station_id);
                if(radioStation==null)
                    _logger.LogError("Radio Station not found | Build Id: {BuildId}, Module: {Module}", item?.build_id, "Playout");

                //-- Get playout session tracks
                var playoutSessionTracks = await _unitOfWork.PlayOut.GetAssetAvilablePlayoutTracksBySessionId(item.id);
                string deliveryLocation = radioStation.delivery_location;
                List<PlayoutTrack> PlayoutTracks = new List<PlayoutTrack>(); 

                if (playoutSessionTracks.Count() == 0)
                {
                    _logger.LogError("No tracks found | Build Id: {BuildId}, Module: {Module}", item?.build_id, "Playout");
                    await _unitOfWork.PlayOut.UpdatePublishStatus(new playout_session()
                    {
                        id = item.id,
                        publish_status = (int)enPlayoutSessionPublishStatus.PUBLISH_FAILED
                    });
                }
                else {
                    foreach (var sessionTrack in playoutSessionTracks)
                    {
                        var track = await _elasticLogic.GetElasticTrackDocByIdForPlayout(sessionTrack.track_id);
                        if (track == null)
                        {
                            _logger.LogError("Track not found in Elastic | Build Id: {BuildId}, Track Id: {BuildId}, Module: {Module}", item?.build_id, sessionTrack?.dh_track_id, "Playout");
                        }
                        else
                        {
                            PlayoutTracks.Add(new PlayoutTrack
                            {
                                id = sessionTrack.id,
                                trackId = Guid.Parse(sessionTrack.dh_track_id),
                                type = sessionTrack.track_type.ToLower(),
                                prsPublishers = track.prsWorkPublishers,
                                mlTrackId = track.id
                            });
                            if (radioStation.delivery_location != radioStation.delivery_location_classical
                                && sessionTrack.track_type.ToLower() == "classical")
                            {
                                deliveryLocation = radioStation.delivery_location_classical;
                            }
                        }
                    }
                    if (PlayoutTracks.Count() > 0)
                        await ProcessAndSendSigniantRequest(CreateRequestMessage(item, deliveryLocation, radioStation.station, PlayoutTracks), item);
                }                
            }
        }

        private RequestMessage CreateRequestMessage(playout_session playoutSession,
            string location,string station, List<PlayoutTrack> playoutTracks)
        {
            return new RequestMessage()
            {
                buildId = (Guid)playoutSession.build_id,
                channels = new List<PlayoutChannel> {
                            new PlayoutChannel
                            {
                                deliveryLocation =  location,
                                name = station
                            }
                        },
                tracks = playoutTracks
            };
        }

        private async Task ProcessAndSendSigniantRequest(RequestMessage req,
            playout_session playoutSession)
        {
            try
            {

                bool sendSigniantRequest = false;
                PlayoutDelivery request = new PlayoutDelivery()
                {
                    id = req.buildId,
                    trackFiles = new List<PlayoutDeliveryTrackFiles>(),
                    deliveryLocation = req.channels[0].deliveryLocation,
                };
                foreach (var item in req.tracks)
                {
                    Stream xmlStream = await GetPlayoutXMLStream(playoutSession.build_id, item.trackId, item.mlTrackId, item.type);

                    await _unitOfWork.PlayOut.UpdateTrackXmlStatus(new playout_session_tracks()
                    {
                        id = item.id,
                        xml_status = xmlStream == null ? (int)enPlayoutTrackXMLStatus.CREATE_FAILED : (int)enPlayoutTrackXMLStatus.CREATED
                    });

                    TranscodeResponse transcodeResponse = await GetWavFromTranscodeAPI(item.trackId);

                    request.trackFiles.Add(new PlayoutDeliveryTrackFiles()
                    {
                        id = item.id,
                        xmlStream = xmlStream,
                        trackId = item.trackId,
                        wav = transcodeResponse?.assetUrl,
                        signedWav = transcodeResponse?.preSignedUrl
                    });
                }

                var uploadResult = await UploadFilesToPlayoutS3(request, playoutSession);

                //---- Remove error tracks from the playout request
                if (uploadResult.errorTracks.Count() > 0)
                {
                    if (playoutSession.publish_attempts < 2)
                    {
                        playoutSession.publish_status = (int)enPlayoutSessionPublishStatus.REPUBLISH;
                        await _unitOfWork.PlayOut.UpdatePublishStatus(playoutSession);

                        _logger.LogInformation("Set to republish attempts: {Attempts} | BuildId: {BuildId} , Module: {Module}", playoutSession.publish_attempts + 1, request.id, "Playout");
                    }
                    else
                    {
                        request.trackFiles.RemoveAll(a => uploadResult.errorTracks.Contains(a.trackId));
                        sendSigniantRequest = true;
                    }
                }
                else
                {
                    sendSigniantRequest = true;
                }
                //-------------------------------------------------

                playoutSession.publish_attempts = playoutSession.publish_attempts + 1;
                await _unitOfWork.PlayOut.UpdateAttempts(playoutSession);

                if (sendSigniantRequest)
                {
                    if (request.trackFiles?.Count() > 0)
                    {
                        await CreateSigniantJob(request, uploadResult.totalFileSize, playoutSession);
                    }
                    else
                    {
                        await _unitOfWork.PlayOut.UpdatePublishStatus(new playout_session()
                        {
                            id = playoutSession.id,
                            publish_status = (int)enPlayoutSessionPublishStatus.PUBLISH_FAILED
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessAndSendSigniantRequest | Build Id: {BuildId}, Module: {Module}", playoutSession?.build_id, "Playout");
            }                          
        }

        private async Task<Stream> GetPlayoutXMLStream(Guid? buildId, Guid dhTrackId, Guid mlTrackId, string trackType)
        {
            try
            {
                ml_master_track mlMasterTrack = await _unitOfWork.MLMasterTrack.GetMaterTrackById(dhTrackId);

                EXPORT eXPORT = await CreateEXPORT(dhTrackId, mlTrackId, trackType, mlMasterTrack);

                if (eXPORT == null)
                    return null;

                var memStream = new MemoryStream();

                var xmlWriter = new StreamWriter(memStream, Encoding.UTF8);
                using var writer = new SanitisingXmlWriter(XmlWriter.Create(xmlWriter, new XmlWriterSettings { Indent = true }));
                new XmlSerializer(typeof(EXPORT)).Serialize(writer, eXPORT);                

                return memStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPlayoutXMLStream | Build Id: {BuildId}, Track Id: {Track}, Module: {Module}", buildId, dhTrackId, "Playout");
                return null;
            }            
        }

        private async Task CreateSigniantJob(PlayoutDelivery request, long totalFileSize, playout_session playoutSession)
        {
            try
            {
                //Signiant Job Id only allows alphanumeric and underscore characters
                var signiantRequestId = $"{request.id}_{CommonHelper.RandomString(8)}";
                var sanitizedRequestId = signiantRequestId.Replace('-', '_');

                if (!string.IsNullOrEmpty(playoutSession.signiant_ref_id))
                {
                    sanitizedRequestId = playoutSession.signiant_ref_id;

                    await RestartSigniantJob(sanitizedRequestId, playoutSession);

                    return;
                }

                var jobRequest = CreateTransferJob(request, sanitizedRequestId,
                    totalFileSize, playoutSession);

                jobRequest.TransferJob.BmsResourceID = sanitizedRequestId;
                jobRequest.TransferJob.BmsNotifyAt = new BmsNotifyAt
                {
                    BmsReplyTo = $"{_appSettings.Value.SigniantConfigs.ReplyBaseUrl}/bbc/playout/monitoring/reply/{request.id}",
                    BmsFaultTo = $"{_appSettings.Value.SigniantConfigs.ReplyBaseUrl}/bbc/playout/monitoring/fault/{request.id}"
                };

                var playoutRequest = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.SigniantConfigs.ServiceBaseUrl}/signiant_fims_transfer_service/transferservice/job");
                playoutRequest.Headers.Add("X-FIMS-UserName", _appSettings.Value.SigniantConfigs.Username);
                playoutRequest.Headers.Add("X-FIMS-Password", _appSettings.Value.SigniantConfigs.Password);

                playoutRequest.Content = new StringContent(JsonConvert.SerializeObject(jobRequest), Encoding.UTF8, "application/json");

                HttpResponseMessage response = null;
                HttpClient httpClient;
                string responseString = string.Empty;               
                
                httpClient = _clientFactory.CreateClient();
                response = await httpClient.SendAsync(playoutRequest);                

                if (response.IsSuccessStatusCode)
                {
                    responseString = await response.Content.ReadAsStringAsync();

                    await _unitOfWork.PlayOut.UpdateSigniantRefId(new playout_session()
                    {
                        id = playoutSession.id,
                        signiant_ref_id = sanitizedRequestId
                    });

                    DeliveryJobResponse playoutDeliveryJobResponse = JsonConvert.DeserializeObject<DeliveryJobResponse>(responseString);

                    await _unitOfWork.PlayOut.SavePlayoutResponse(new playout_response()
                    {
                        build_id = (Guid)playoutSession.build_id,
                        response_time = CommonHelper.GetCurrentUtcEpochTimeMicroseconds(),
                        status = "delivered_awaiting_signiant"
                    });

                    await _unitOfWork.PlayOut.UpdatePublishStatus(new playout_session()
                    {
                        id = playoutSession.id,
                        publish_status = (int)enPlayoutSessionPublishStatus.PUBLISH_DONE
                    });
                }
                else
                {
                    FaultJobResponse playoutFaultJobResponse = JsonConvert.DeserializeObject<FaultJobResponse>(responseString);

                    await _unitOfWork.PlayOut.UpdatePublishStatus(new playout_session()
                    {
                        id = playoutSession.id,
                        publish_status = (int)enPlayoutSessionPublishStatus.PUBLISH_FAILED
                    });

                    _logger.LogError(
                        "Error occurred attempting to create Signiant Job, Code: {ErrorCode}, Description: {ErrorDescription}, Detail: {ErrorDetail}, {BuildId}",
                        playoutFaultJobResponse.Fault.BmsCode,
                        playoutFaultJobResponse.Fault.BmsDescription,
                        playoutFaultJobResponse.Fault.BmsDetail,
                        request.id
                        );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateSigniantJob failed | BuildId: {BuildId} , Module: {Module}", request.id,"Playout");
            }            
        }

        private async Task RestartSigniantJob(string signiantId, playout_session playoutSession)
        {
            var requetBody = new { jobID = signiantId, jobCommand = "restart" };

            var playoutRequest = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.Value.SigniantConfigs.ServiceBaseUrl}/signiant_fims_transfer_service/transferservice/job/{signiantId}/manage");
            playoutRequest.Headers.Add("X-FIMS-UserName", _appSettings.Value.SigniantConfigs.Username);
            playoutRequest.Headers.Add("X-FIMS-Password", _appSettings.Value.SigniantConfigs.Password);

            playoutRequest.Content = new StringContent(JsonConvert.SerializeObject(requetBody), Encoding.UTF8, "application/json");

            var client = _clientFactory.CreateClient();

            _logger.LogInformation("Sending Reset Signiant job request for request {RequestId}} , Module: {Module}", signiantId, "Playout");

            var response = await client.SendAsync(playoutRequest);
            var responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {               
                await _unitOfWork.PlayOut.UpdatePublishStatus(new playout_session()
                {
                    id = playoutSession.id,
                    publish_status = (int)enPlayoutSessionPublishStatus.PUBLISH_DONE
                });

                _logger.LogInformation("Received Reset Signiant job {SigniantJobId} for request {RequestId}",
                    signiantId, playoutSession.build_id);
            }
            else
            {
                FaultJobResponse playoutFaultJobResponse = JsonConvert.DeserializeObject<FaultJobResponse>(responseString);

                await _unitOfWork.PlayOut.UpdatePublishStatus(new playout_session()
                {
                    id = playoutSession.id,
                    publish_status = (int)enPlayoutSessionPublishStatus.PUBLISH_FAILED
                });

                _logger.LogError("Error Reset Signiant job {SigniantJobId} for request {RequestId}",
                   signiantId, playoutSession.build_id);
            }
        }

        private JobRequest CreateRestartJob(PlayoutDelivery request,
            string requestId, long totalFileSize)
        {
            JobRequest job = new JobRequest()
            {
                TransferJob = new TransferJob()
                {
                    BmsBmObjects = new BmsBmObjects()
                    {
                        BmsBmObject = new List<BmsBmObject>()
                    }
                }
            };

            BmsBmObject bmsBmObject = new BmsBmObject()
            {
                BmsBmContents = new BmsBmContents() { BmsBmContent = new List<BmsBmContent>() }
            };

            BmsBmContent bmsBmContent = new BmsBmContent()
            {
                BmsBmContentFormats = new BmsBmContentFormats() { BmsBmContentFormat = new List<BmsBmContentFormat>() }
            };

            BmsBmContentFormat format = new BmsBmContentFormat()
            {
                BmsBmEssenceLocators = new BmsBmEssenceLocators()
                {
                    BmsBmEssenceLocator = new List<BmsBmEssenceLocator>()
                    {
                        new BmsBmEssenceLocator
                        {
                            XsiType
                                = "bms.SimpleFileLocatorType",
                            BmsFile
                                = _appSettings.Value.SigniantConfigs.SimpleFileLocatorPath
                        }
                    }
                }
            };
            format.BmsPackageSize = totalFileSize;

            bmsBmContent.BmsBmContentFormats.BmsBmContentFormat.Add(format);

            bmsBmObject.BmsBmContents.BmsBmContent.Add(bmsBmContent);

            job.TransferJob.BmsBmObjects.BmsBmObject.Add(bmsBmObject);
            job.TransferJob.BmsPriority = "low";
            job.TransferJob.Profiles = new Profiles() { transferProfile = new List<TransferProfile>() };

            TransferProfile profile = new TransferProfile()
            {
                BmsLocation =
                    $"{_appSettings.Value.SigniantConfigs.ServiceBaseUrl}/template/{_appSettings.Value.SigniantConfigs.TransferTemplate}/component/Flight_Download",
                BmsExtensionGroup = new BmsExtensionGroup(),
                transferAtom = new List<TransferAtom>
                {
                    new TransferAtom(){ BmsDestination = _appSettings.Value.SigniantConfigs.SimpleFileLocatorPath}
                }
            };
            profile.BmsExtensionGroup.SigSigniantExtensionGroup = new List<SigSigniantExtensionGroup>();

            SigSigniantExtensionGroup sigSigniantExtensionGroup = new SigSigniantExtensionGroup()
            {
                SigJobContextParameters =
                    new SigJobContextParameters
                    {
                        SigJobGroup = _appSettings.Value.SigniantConfigs.JobGroup,
                        SigJobName = $"Soundmouse_{requestId}"
                    },
                SigJobVariables = new List<SigJobVariable>()
            };
            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.sourceOptions.sourceData",
                SigJobVariableValue = string.Join(',',
                    request.trackFiles.Select(t => $"{request.id}/{t.trackId}.wav,{request.id}/{t.trackId}.xml"))
            });
            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.sourceOptions.flightStorageConfigID",
                SigJobVariableValue = _appSettings.Value.SigniantConfigs.FlightStorageConfigId
            });
            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.targetOptions.targetAgent",
                SigJobVariableValue = _appSettings.Value.SigniantConfigs.TargetAgent
            });
            sigSigniantExtensionGroup.SigJobVariables.Add(new SigJobVariable
            {
                SigJobVariableName = "Flight_Download.targetOptions.targetFolder",
                SigJobVariableValue = request.deliveryLocation
            });


            profile.BmsExtensionGroup.SigSigniantExtensionGroup.Add(sigSigniantExtensionGroup);
            job.TransferJob.Profiles.transferProfile.Add(profile);

            return job;
        }

        public async Task S3Cleanup()        
        {
            _logger.LogInformation("S3Cleanup process started | Module: {Module}", "Playout");
            var playouts = await _unitOfWork.PlayOut.GetS3CleanupSessions();
            foreach (var session in playouts)
            {
                var playoutTracks = await _unitOfWork.PlayOut.GetPlayoutTracksBySessionId(session.id);
                List<string> keys = new List<string>();

                foreach (var track in playoutTracks)
                {
                    keys.Add($"{session.build_id}/{track.dh_track_id}.xml");
                    keys.Add($"{session.build_id}/{track.dh_track_id}.wav");
                }

                if (await _deliveryDestinationS3ClientRepository.DeleteByKeys(keys))
                {
                    _logger.LogInformation("S3Cleanup process success | BuildId: {BuildId} , Module: {Module}", session.build_id, "Playout");
                    await _unitOfWork.PlayOut.UpdateS3Cleanup(session.id);
                }
                else {
                    _logger.LogError("S3Cleanup failed | BuildId: {BuildId} , Module: {Module}", session.build_id,  "Playout");
                }
            }            
        }

        public async Task UpdateSigniantReplay(Guid requestId,SigniantReplyResponse signiantReplyResponse)
        {
            _logger.LogInformation("Received update for request with id {requestId}, Signiant Job ID {SigniantJobId}, Status: {JobStatus} , Module: {Module}",
             requestId, signiantReplyResponse.TransferJob.BmsServiceProviderJobID, signiantReplyResponse.TransferJob.BmsStatus, "Playout Signiant");

            await ProcessSigniantMessage(requestId, signiantReplyResponse.TransferJob.BmsStatus, JsonConvert.SerializeObject(signiantReplyResponse));
        }

        public async Task UpdateSigniantFault(Guid requestId, SigniantFaultResponse signiantFaultResponse)
        {
            var job = signiantFaultResponse.TransferFaultNotificationType.TransferJob;
            var fault = signiantFaultResponse.TransferFaultNotificationType.Fault;

            _logger.LogInformation("Received fault for request with id {requestId} Signiant Job Id {SigniantJobId}, Status: {JobStatus}, Code: {JobCode}, Description: {FaultDescription} , Module: {Module}",
                requestId, job.BmsServiceProviderJobID, job.BmsStatus, fault.BmsCode, fault.BmsDescription, "Playout Signiant");

            await ProcessSigniantMessage(requestId, job.BmsStatus, JsonConvert.SerializeObject(fault));
        }

        private async Task ProcessSigniantMessage(Guid buildId,string status,string message)
        {
            playout_session playoutSession = await _unitOfWork.PlayOut.GetPlayoutSessionByBuildId(buildId);

            if (playoutSession != null)
            {
                playout_response lastResponse = await _unitOfWork.PlayOut.GetTheLastResponse(buildId);

                //Save Response to DB
                playout_response playout_response = new playout_response
                {
                    build_id = buildId,                  
                    response_json = message,
                    status = $"signiant_{status}",
                    response_time = CommonHelper.GetCurrentUtcEpochTimeMicroseconds()
                };
                await _unitOfWork.PlayOut.SavePlayoutResponse(playout_response);

                //Update Session Status               
                int? publishStatus = null;

                switch (playout_response.status)
                {
                    case "signiant_completed":
                        publishStatus = (int)enPlayoutSessionPublishStatus.SIGNIANT_SUCCESS;
                        break;
                   
                    case "signiant_failed":
                        publishStatus = (int)enPlayoutSessionPublishStatus.SIGNIANT_FAILED;
                        break;

                    case "signiant_running":
                        publishStatus = (int)enPlayoutSessionPublishStatus.SIGNIANT_RUNNING;
                        break;
                }

                playoutSession.publish_status = publishStatus;              

                if (playoutSession.publish_status != null && 
                    (lastResponse == null || lastResponse.response_time < playout_response.response_time))
                    await _unitOfWork.PlayOut.UpdatePublishStatus(playoutSession);               
            }
            else
            {
                _logger.LogWarning("Unknown Playout Response Message | Build Id: {buildId} , Module: {Module}", buildId, "Playout");
            }
        }

        public async Task<int> RestartPlayout(int plaoutId)
        {
           return await _unitOfWork.PlayOut.RestartPlayout(plaoutId);
        }
    }
   
}
