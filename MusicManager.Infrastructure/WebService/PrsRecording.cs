using Microsoft.Extensions.Caching.Memory;
using MusicManager.PrsSearch.Models;
using MusicManager.PrsSearch.Recording;
using MusicManager.PrsSearch.Search.Recording;
using Soundmouse.Messaging.Model;
using MusicManager.Application.WebService;
using MusicManager.Core.ViewModules;
using Microsoft.Extensions.Logging;
using System;
using Soundmouse.Matching.Prs.Search.Work;

namespace MusicManager.Infrastructure.WebService
{
    public class PrsRecording: IPrsRecording
    {        
        private readonly PrsSearch.PrsAuth.IAuthentication _authentication;
        private readonly ILogger<PrsWorkDetails> _logger;

        public PrsRecording(
            PrsSearch.PrsAuth.IAuthentication authentication,
            ILogger<PrsWorkDetails> logger)
        {          
            _authentication = authentication;
            _logger = logger;
        }

        public Recording[] GetRecordingByIsrc(string isrc,Guid? trackId = null)
        {
            string token = _authentication.GetSessionToken();
            var recordings = Isrc.GetRecordings(token,isrc);

            int count = recordings==null ? 0 : recordings.Length;
            var pageCount = (count / 20) + 1;

            _logger.LogInformation("ISRC: {ISRC}, TrackId: {TrackId}, Recording Count: {Count},Page Count: {PageCount} | Module: {Module}", 
                isrc, trackId, count, pageCount, "PRS Search - Recording ISRC");

            return recordings;
        }

        public Recording[] GetRecordingsByTitleArtist(string title, string artists, Guid? trackId = null)
        {
            string token = _authentication.GetSessionToken();
            var recordings = TitleArtist.GetRecordings(token,title, artists);

            int count = recordings == null ? 0 : recordings.Length;
            var pageCount = (count / 20) + 1;

            _logger.LogInformation("Title - Artist: {TitleArtist}, TrackId: {TrackId}, Recording Count: {Count}, Page Count: {PageCount} | Module: {Module}",
                title + " - " + artists, trackId, count, pageCount, "PRS Search - Recording Title Artist");

            return recordings;
        }

        public Recording[] GetRecordingsByTitle(string title)
        {
            string token = _authentication.GetSessionToken();
            var recordings = PrsSearch.Search.Recording.Title.GetRecordings(token,title);
            return recordings;
        }

        public Track GetRecordingMatches(Track track, Recording[] recordings)
        {
            return TitleArtist.GetRecordingMatches(track, recordings);
        }

        public Track GetTrackMatches(Track mlTrack, Track[] prsTracks)
        {
            return TitleArtist.GetTrackMatches(mlTrack, prsTracks);
        }       
    }
}
