using Microsoft.Extensions.Logging;
using MusicManager.Application.WebService;
using System;

namespace MusicManager.Infrastructure.WebService
{
    public class PrsWorkDetails: IPrsWorkDetails
    {
        private readonly PrsSearch.PrsAuth.IAuthentication _authentication;
        private readonly ILogger<PrsWorkDetails> _logger;

        public PrsWorkDetails(
            PrsSearch.PrsAuth.IAuthentication authentication, ILogger<PrsWorkDetails> logger)
        {
            _authentication = authentication;
            _logger = logger;
        }

        public MusicManager.PrsSearch.Models.Work GetWorkDetailsByTuneCode(string tuneCode, Guid? dhTrackId, int retries = 1)
        {
            try
            {
                string token = _authentication.GetSessionToken();
                var work = MusicManager.PrsSearch.WorkDetail.Tunecode.GetWork(token, tuneCode);
                _logger.LogInformation("Tunecode: {Tunecode}, TrackId: {TrackId}, Works Count: {Count} | Module: {Module}",
                    work == null ? 0 : 1, tuneCode, dhTrackId, "PRS Search - Work Details");
                return work;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetWorkDetailsByTuneCode | Tunecode: {Tunecode}, TrackId: {TrackId} | Module: {Module}", tuneCode, dhTrackId, "PRS Search");
                return null;
            }
        }
    }
}
