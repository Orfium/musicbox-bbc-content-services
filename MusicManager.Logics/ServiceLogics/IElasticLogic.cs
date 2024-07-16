using MusicManager.Core.Models;
using MusicManager.Core.Payload;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Logics.ServiceLogics
{
    public interface IElasticLogic
    {
        Task<bool> TrackIndex(MLTrackDocument mLTrackDocument);
        Task<bool> AlbumIndex(MLAlbumDocument mLAlbumDocument);
        Task<bool> UpdateIndex(MLTrackDocument mLTrackDocument);
        Task<MLTrackDocument> GetElasticTrackDocById(Guid trackDocId);
        Task<MLTrackDocument> GetElasticTrackDocByDhTrackId(Guid trackDocId);
        Task<MLAlbumDocument> GetElasticAlbumByProdId(Guid prodId);
        Task<MLTrackDocument> GetElasticTrackDocByIdForPlayout(Guid trackDocId);
        Task<List<MLTrackDocument>> SearchTracksForIndexCtags(int size, Guid indexedCtagIdetifier);
        Task<List<MLTrackDocument>> SearchTracksForPRSIndex(int size);
        Task<List<MLTrackDocument>> SearchTracksForPRSIndex2(int size);
        Task<SearchData<List<MLTrackDocument>>> GetAllMasterTracksByAlbumId(Guid albumId, string orgId, int retries = 3);
        Task<(string error, Guid id, string reason)[]> BulkIndexTrackDocument(List<MLTrackDocument> trackDocuments, int retries = 3);
        Task<(string error, Guid id, string reason)[]> BulkIndexAlbumDocument(List<MLAlbumDocument> albumDocuments, string orgId, int retries = 3);
        Task<long> GetValidIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetPRSIndexedCount(TrackCountPayload trackCountPayload);
        Task<long> GetPRSNotMatchedCount(TrackCountPayload trackCountPayload);
        Task<long> GetArchiveIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetRestrictIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetRestrictAlbumIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetSourceIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetArchiveAlbumIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetSourceDeletedAlbumIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetValidAlbumIndexCount(TrackCountPayload trackCountPayload);
        Task<long> GetIndexCtagCompletedCount(TrackCountPayload trackCountPayload, c_tag_index_status cTagIndexStatus);
        Task<IEnumerable<MLTrackDocument>> GetTrackElasticTrackListByIds(Guid[] trackIds, int retries = 3);
        Task RestraictTracks(Guid[] trackIds,bool restrict);
        Task ArchiveTracks(Guid[] trackIds,bool archived);
        Task RestoreTracks(Guid[] trackIds, bool archived);
        Task DeleteTracks(Guid[] trackIds);
        Task CreateTrackIndex();
        Task CreateAlbumIndex();
        Task<List<MLTrackDocument>> SearchCtagedDocs(int size);
        Task<List<ServiceLog>> GetServiceStatus();
        Task<IEnumerable<MLTrackDocument>> SearchTracksForPRSIndex(Guid[] trackIds);
        Task<IEnumerable<MLTrackDocument>> SearchTakedownTracks(int size);
        Task<IEnumerable<MLTrackDocument>> SearchPreReleaseTracks(int size);
        Task<IEnumerable<MLTrackDocument>> SearchNotPreReleaseTracks(int size);
        Task<long> GetTakedownTracksCount();
        Task<long> GetPreReleaseTracksCount();
        Task<long> GetNotPreReleaseTracksCount();
        Task<bool> ResetServiceLogger();
        Task DeleteAlbum(Guid albumId);
        Task<long> GetTrackCountByQuery(string query);
        Task<long> GetTrackCountByQueryAndDate(string query,DateTime date);
        Task<IEnumerable<MLTrackDocument>> SearchLiveTracks();
    }
}
