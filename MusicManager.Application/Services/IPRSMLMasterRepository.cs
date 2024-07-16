using MusicManager.Core.PrsModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Application.Services
{
    public interface IPRSMLMasterRepository
    {
        Task SaveTrackPrsMaster(track_prs_master track_Prs_Master);
    }
}
