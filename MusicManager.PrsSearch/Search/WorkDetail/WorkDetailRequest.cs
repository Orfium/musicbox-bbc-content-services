using MusicManager.Core.ViewModules;
using MusicManager.PrsSearch;
using Soundmouse.Messaging;
using System;
using System.Linq;
using WorkDetailsServiceReference;

namespace MusicManager.PrsSearch.WorkDetail
{
    public abstract class WorkDetailRequest
    {
        private static readonly GetWorkDetailsSoap _client = new GetWorkDetailsSoapClient(GetWorkDetailsSoapClient.EndpointConfiguration.GetWorkDetailsSoap);


        protected abstract WorkDetailsSearchExtendedRequest CreateRequest();

        public Models.Work GetResults(string sessionId)
        {
            // should return no more than one result
            var request = CreateRequest();

            request.AuthenticationToken = new AuthenticationToken { SessionId = sessionId };
            request.IncludeArtists = "";

            var work = GetResult(request);

            Stats.Increment(work != null
                ? "matching.search.prs-work-detail.found"
                : "matching.search.prs-work-detail.not-found");

            return work != null ? ToWorkDetail(work) : null;
        }

        protected WebServiceWorkDetailsExtendedBO GetResult(WorkDetailsSearchExtendedRequest request,
                                                    int page = 0,
                                                    int retries = 3)
        {
            try
            {
                var response = Stats.Time(() => _client.WorkDetailsSearchExtendedAsync(request).Result.WorkDetailsSearchExtendedResult,
                    "matching.search.prs-work.requested");

                Stats.Increment("matching.search.prs-work.searched");

                return response;
            }
            catch (Exception ex)
            {
                Stats.Increment("matching.search.prs-work.error");

                if (retries <= 1)
                    throw new PrsServiceException(ex);                

                return GetResult(request, page, retries - 1);
            }
        }

        private Models.Work ToWorkDetail(WebServiceWorkDetailsExtendedBO arg)
        {
            var publishers = arg.PublisherArray.Select(w => new MLInterestedParties(w.Name, w.RoleType, w.PerformingRightAffiliation));
            var writers = arg.WriterArray.Select(w => new MLInterestedParties(w.Name, "composer", w.PerformingRightAffiliation));

            return new MusicManager.PrsSearch.Models.Work
            {
                Publishers = publishers.ToArray(),
                Writers = writers.ToArray(),
                Title = string.Join(", ", arg.TitleArray),
                Tunecode = arg.Tunecode,
                Type = arg.WorkType,
                Iswc = arg.IswcArray,
                LibraryCatalogueNumbers = arg.LibraryCatNumbers,
                MechanicalShareSummary = arg.MechanicalShareSummary,
                PriorApprovalCode = arg.PriorApprovalCode
            };
        }
    }
}