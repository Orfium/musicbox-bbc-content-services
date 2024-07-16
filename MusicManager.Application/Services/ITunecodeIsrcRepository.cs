using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface ITunecodeIsrcRepository : IGenericRepository<isrc_tunecode>
    {
        Task<bool> IsExist(string isrc, string tunecode);
        Task<int> AddIsrcTunecode(string isrc, string tunecode);
        
    }
}
