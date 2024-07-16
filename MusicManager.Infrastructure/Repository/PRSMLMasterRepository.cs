using Microsoft.Extensions.Options;
using MusicManager.Application.Services;
using MusicManager.Core.PrsModels;
using MusicManager.Core.ViewModules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class PRSMLMasterRepository : IPRSMLMasterRepository   {

        public IOptions<AppSettings> _appSettings { get; }

        public PRSMLMasterRepository(IOptions<AppSettings> appSettings) 
        {
            _appSettings = appSettings;
        }

        public Task SaveTrackPrsMaster(track_prs_master track_Prs_Master)
        {
            throw new NotImplementedException();
        }
    }
}
