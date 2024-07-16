using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Q = Nest.Query<MusicManager.Core.ViewModules.MLTrackDocument>;

namespace Elasticsearch.Util
{
    public static class TrackQuery
    {
        //public static QueryContainer AltTitle(string query, double boost)
        //{
        //    return Q.Match(
        //        qs => qs.Field(tr => tr.  .AlternativeTitle)
        //                .Query(query.EscapeQuery()).Boost(boost));
        //}

        //public static QueryContainer FullName(string fullName)
        //{
        //    return Q.QueryString(
        //        qs => qs.Fields(f => f.Field(tr => tr.InterestedParties.Select(ip => ip.FullName)))
        //                .Query(fullName.EscapeQuery()));
        //}

        public static QueryContainer InterestedPartyName(string value, double? boost)
        {
            // Including the boost in the field parameters will cause the query to fail
            var field = new Field("value.trackData.interestedParties.fullName");

            value = value.Replace("!", "");

            // Note: a nested query is created automatically
            return Q.Term(field, value.ToLower(), boost);
        }

        public static QueryContainer Identifier(string identifier, string value)
        {
            return
                Q.QueryString(
                    qs => qs.Fields(new[] { $"value.trackData.identifiers.{identifier}" })
                            .Query(CleanseIdentifier(value))
                            .Boost(2));
        }

        public static QueryContainer Identifier(string identifier, string[] values)
        {
            var field = $"value.trackData.identifiers.{identifier}";

            var query = (QueryContainer)null;

            foreach (var value in values)
            {
                var term = CleanseIdentifier(value);

                query |= Q.Match(
                    mm => mm.Boost(2).MinimumShouldMatch(MinimumShouldMatch.Fixed(1)).Field(field).Query(term));
            }

            return query;
        }

        public static string CleanseIdentifier(string value)
        {
            if (value == null)
                return null;

            var sb = new StringBuilder();

            foreach (var c in value.Where(char.IsLetterOrDigit))
            {
                sb.Append(c);
            }

            return sb.ToString()
                     .ToLowerInvariant();
        }

        //public static QueryContainer Role(string role)
        //{
        //    return Q.QueryString(
        //        qs => qs.Fields(
        //            f => f.Field(tr => tr.InterestedParties.Select(ip => ip.Role)))
        //                .Query(role));
        //}

        public static QueryContainer Title(string query, double boost)
        {
            return Q.Match(
                qs => qs.Field(tr => tr.trackTitle)
                        .Query(query.EscapeQuery()).Boost(boost));
        }

        public static QueryContainer AltTitle(string query, double boost)
        {
            return Q.Match(
                qs => qs.Field(tr => tr.alternativeTitle)
                        .Query(query.EscapeQuery()).Boost(boost));
        }


    }
}
