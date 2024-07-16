using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IWorkspaceLibTracksTobeSyncedRepository : IGenericRepository<ws_lib_tracks_to_be_synced>
    {
        Task<string> GetMlStatusByType(Guid refId,string type);
        Task RemoveUnwantedEntries(Guid refId, string type, enMLStatus enMLStatus);
        Task<List<ws_lib_tracks_to_be_synced>> GetToBeSyncedList();
        Task<int> UpdateStatus(string status, Guid ref_id);
        Task<ws_lib_tracks_to_be_synced> GetByRefId(Guid refId, string type);
    }
}
