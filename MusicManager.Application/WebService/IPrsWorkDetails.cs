using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Application.WebService
{
    public interface IPrsWorkDetails
    {
        MusicManager.PrsSearch.Models.Work GetWorkDetailsByTuneCode(string tuneCode, Guid? dhTrackId = null, int retries = 1);
    }
}
