using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ILibraryStagingRepository : IGenericRepository<staging_library>
    {
        int Truncate();
        Task<int> BulkInsert(List<staging_library> stagingLibrary);        
    }
}
