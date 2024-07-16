using MusicManager.Application.WebService;
using MusicManager.PrsSearch.Work;
using Soundmouse.Matching.Prs.Search.Work;
using Soundmouse.Messaging.Model;

namespace MusicManager.Infrastructure.WebService
{
    public class Work: IWork
    {
        private readonly PrsSearch.PrsAuth.IAuthentication _authentication;
        public Work(
            PrsSearch.PrsAuth.IAuthentication authentication)
        {
            _authentication = authentication;
        }
        public PrsSearch.Models.Work[] GetWorksFromTuneCode(string tuneCode)
        {
            string token = _authentication.GetSessionToken();
            var Works = Tunecode.GetWorks(token,tuneCode);
            return Works;
        }

        public PrsSearch.Models.Work[] GetWorksFromTitle(string[] titles)
        {
            string token = _authentication.GetSessionToken();
            var Works = Title.GetWorks(token,titles);
            return Works;
        }

        public PrsSearch.Models.Work[] GetWorksByTitleWriters(string title, string writers)
        {
            string token = _authentication.GetSessionToken();
            var Works = TitleWriter.GetWorks(token, title, writers);
            return Works;
        }

        public PrsSearch.Models.Work GetMatchingWork(Track track, PrsSearch.Models.Work[] works)
        {
            throw new System.NotImplementedException();
        }
    }
}
