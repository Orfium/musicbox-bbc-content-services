using MusicManager.PrsSearch.Models;
using Soundmouse.Messaging.Model;

namespace MusicManager.Application.WebService
{
    public interface IWork
    {
        Work[] GetWorksFromTitle(string[] titles);
        Work[] GetWorksFromTuneCode(string tuneCode);
        Work[] GetWorksByTitleWriters(string title, string writers);
        Work GetMatchingWork(Track track, Work[] works);
    }
}
