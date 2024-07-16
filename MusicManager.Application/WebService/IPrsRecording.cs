using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Application.WebService
{
    public interface IPrsRecording
    {
        PrsSearch.Models.Recording[] GetRecordingByIsrc(string isrc, Guid? trackId = null);
        PrsSearch.Models.Recording[] GetRecordingsByTitle(string title);
        PrsSearch.Models.Recording[] GetRecordingsByTitleArtist(string title, string artists, Guid? trackId = null);
        Track GetRecordingMatches(Track track, PrsSearch.Models.Recording[] recordings);
        Track GetTrackMatches(Track mlTrack, Track[] prsTracks);
    }
}
