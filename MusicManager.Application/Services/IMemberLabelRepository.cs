using MusicManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IMemberLabelRepository: IGenericRepository<member_label>
    {
        Task InsertManualLabel(member_label label);
        Task UpdateLabel(member_label label);
    }
}
