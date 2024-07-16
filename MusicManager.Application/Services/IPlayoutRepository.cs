using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IPlayoutRepository : IGenericRepository<playout_session>
    {
        Task<playout_session> SavePlayoutSession(playout_session playout);
        Task<int> SavePlayoutSessionTracks(playout_session_tracks tracks);
        Task<radio_stations> GetRadioStationById(Guid id);
        Task SavePlayoutResponse(playout_response response);
        Task<playout_response> GetTheLastResponse(Guid buildId);
        Task<int> DeletePlayoutTracks(long id);
        Task<playout_session> GetPlayoutSessionById(int id);
        Task<int> UpdatePlayoutSessionStatus(playout_session playoutSession);
        Task<playout_session> GetPlayoutSessionByBuildId(Guid buildId);
        Task<playout_session_tracks> GetPlayoutTrackById(int id);
        Task<int> UpdateTrackTypeById(int id, string trackType, enPlayoutTrackStatus enPlayoutTrackStatus);
        Task<int> UpdatePlayoutSessionById(playout_session playoutSession);
        Task<int> UpdatePlayoutSessionPublishStatus(playout_session playoutSession);
        Task<IEnumerable<playout_session_tracks>> GetPlayoutTracksBySessionId(int sessionId);
        Task<IEnumerable<playout_session_tracks>> GetAssetAvilablePlayoutTracksBySessionId(int sessionId);
        Task<IEnumerable<playout_session>> GetPendingPlayoutsessions();
        Task<IEnumerable<playout_session>> GetPlayoutSessionsForPublish();
        Task<int> UpdateSigniantRefId(playout_session playoutSession);
        Task<int> UpdateAttempts(playout_session playoutSession);
        Task<int> UpdatePublishStatus(playout_session playoutSession);
        Task<int> UpdateTrackXmlStatus(playout_session_tracks playoutSessionTracks);
        Task<int> UpdateTrackAssetStatus(playout_session_tracks playoutSessionTracks);
        Task<int> UpdateTrackStatus(playout_session_tracks playoutSessionTracks);
        Task<int> UpdateS3Cleanup(int id);
        Task<IEnumerable<playout_session>> GetS3CleanupSessions();
        Task<int> UpdatePublishStartTime(int id);
        Task<int> RestartPlayout(int playoutId);
    }
}
