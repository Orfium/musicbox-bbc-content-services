using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ISearchAPIRepository
    {
        Task<List<Guid>> CheckDeletedTracks(List<Guid> trackIds);
    }

}
