using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching
{
    public static class InterestedPartyExtensions
    {
        public static ICollection<Name> GenerateNameParts(this InterestedParty ip)
        {
            if (ip.NameParts != null && ip.NameParts.Count != 0)
                return ip.NameParts;

            var parts = ip.FullName.Replace(".", " ")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var commaParts = parts.Select(p => p.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();

            switch (commaParts.Length)
            {
                case 0:
                    ip.NameParts = new Name[0];
                    break;
                case 1:
                    ip.NameParts = ParseFirstLast(commaParts[0]).ToArray();
                    break;
                default:
                    ip.NameParts = ParseLastFirst(commaParts[0], commaParts[1]).ToArray();
                    break;
            }

            return ip.NameParts;
        }


        private static IEnumerable<Name> ParseFirstLast(string[] words)
        {
            // pattern: [first [middle..n]] last

            if (words.Length == 0)
                yield break;

            if (words.Length > 1)
                yield return Name.First(words.First());

            for (var i = 1; i < words.Length - 1; i++)
            {
                yield return Name.Middle(words[i]);
            }

            yield return Name.Last(words.Last());
        }

        private static IEnumerable<Name> ParseLastFirst(string[] lastWords, string[] firstWords)
        {
            if (firstWords.Any())
            {
                yield return Name.First(firstWords[0]);

                for (int i = 1; i < firstWords.Length; i++)
                {
                    yield return Name.Middle(firstWords[i]);
                }
            }

            if (lastWords.Any())
            {
                foreach (var value in lastWords)
                {
                    yield return Name.Last(value);
                }
            }
        }
    }
}
