using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching
{
    public enum MatchType
    {
        NotMatched = 0,
        PotentialMatch = 1,
        Matched = 2
    }

    public static class MatchTypeGroup
    {
        public static readonly MatchType[] All = { MatchType.NotMatched, MatchType.PotentialMatch, MatchType.Matched };
        public static readonly MatchType[] AnyMatch = { MatchType.PotentialMatch, MatchType.Matched };
        public static readonly MatchType[] Match = { MatchType.Matched };
        public static readonly MatchType[] NonMatch = { MatchType.NotMatched };
    }
}
