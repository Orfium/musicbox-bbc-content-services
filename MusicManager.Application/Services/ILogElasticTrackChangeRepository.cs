using MusicManager.Core.Models;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ILogElasticTrackChangeRepository : IGenericRepository<elastic_track_change>
    {
        Task<IEnumerable<LogElasticTrackChange>> Search(int pageSize, Guid workspace_id,int retries = 2);
        Task<IEnumerable<LogElasticAlbumChange>> SearchElasticAlbumChange(int pageSize, Guid workspace_id, int retries = 2);

        Task<int> BulkDelete(Guid orgWorkspaceId, long lastIndexId);

        Task<int> BulkDeleteAlbums(List<MLAlbumDocument> logElasticAlbumChanges);

        Task LogErrors(List<log_track_index_error> log_Track_Index_Errors);
    }
}
