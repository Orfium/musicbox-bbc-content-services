using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IOrgExcludeRepository : IGenericRepository<org_exclude>
    {
        Task<org_exclude> GetByRefIdAndType(Guid refId,string type);
        Task<int> Save(org_exclude org_Exclude);
        Task<int> Delete(org_exclude org_Exclude);
    }
}
