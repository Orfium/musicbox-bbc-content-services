namespace MusicManager.PrsSearch
{
    public static class PrsLogger
    {
        public static void LogSearchPerformed(string searchType, string queryType, int hitCount, string searchQuery)
        {
            //--- Log PRS search info
            Serilog.Log.Logger.Debug(
                "Search type: {Prs_SearchType}. Query: {Prs_QueryType}. Found {Prs_HitCount} results for query {Prs_SearchQuery}.",
                searchType,
                queryType,
                hitCount,
                searchQuery);
        }
        
    }

    public static class SearchType
    {
        public static readonly string Product = "product";
        public static readonly string Recording = "recording";
        public static readonly string Work = "work";
        public static readonly string WorkDetail = "work_detail";
    }
}