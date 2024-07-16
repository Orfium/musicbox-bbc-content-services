using MusicManager.PrsSearch.DataMatching.Util;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MusicManager.PrsSearch.DataMatching
{
    public class TrackCompare
    {
        private readonly MatchConditions _matchConditions;

        private readonly TrackData _td1, _td2;

        public Track SearchTrack { get; set; }
        public Track MatchTrack { get; set; }

        private TrackCompare(Track searchTrack, Track matchTrack, MatchConditions matchConditions)
        {
            if (searchTrack == null)
                throw new ArgumentNullException(nameof(searchTrack));
            if (matchTrack == null)
                throw new ArgumentNullException(nameof(matchTrack));

            SearchTrack = searchTrack;
            MatchTrack = matchTrack;

            _matchConditions = matchConditions;

            _td1 = searchTrack.TrackData;
            _td2 = matchTrack.TrackData;
        }

        public IDictionary<MatchAttribute, float> AttributeScores { get; set; }
        public MatchAttribute[] MatchAttributes { get; set; }
        public Dictionary<MatchAttribute, float> MatchConditions { get; set; }
        public MatchType MatchType { get; set; }

        public float OverallScore => _matchConditions.AttributeWeights.Sum(w => AttributeScores[w.Key] * w.Value);

        public float NormalisedOverallScore
            => OverallScore / (Math.Max(1, _matchConditions.AttributeWeights.Values.Sum()));


        private bool AssignMatch()
        {
            if (_matchConditions.ProductMustMatch && !IsProductMatch())
                return false;

            foreach (var c in _matchConditions.Conditions)
            {
                if (c.All(fld => AttributeScores[fld.Key] >= fld.Value))
                {
                    MatchConditions = c;
                    MatchAttributes = c.Keys.ToArray();

                    // if isrc/iswc exist but either does not match, demote to potential match
                    if ((IdentifierExists("isrc") && !CompareIdentifier("isrc")) ||
                        (IdentifierExists("iswc") && !CompareIdentifier("iswc")))
                    {
                        MatchType = MatchType.PotentialMatch;
                    }
                    else
                    {
                        MatchType = MatchType.Matched;
                    }

                    return true;
                }
            }

            return false;
        }

        private void CalculatePercentages()
        {
            AttributeScores = new Dictionary<MatchAttribute, float>
            {
                {MatchAttribute.CatNo, CompareProductIdentifier("catalogue_number", true) ? 1 : 0},
                {MatchAttribute.Composer, CompareInterestedParties("composer")},
                {MatchAttribute.Isrc, CompareIdentifier("isrc") ? 1 : 0},
                {MatchAttribute.Iswc, CompareIdentifier("iswc") ? 1 : 0},
                {MatchAttribute.Label, CompareInterestedParties("record_label")},
                {MatchAttribute.Performer, CompareInterestedParties("performer")},
                {MatchAttribute.ProductTitle, CompareProductTitle()},
                {MatchAttribute.Title, CompareTitle()},
                {MatchAttribute.Tunecode, CompareIdentifier("prs") ? 1 : 0},
                {MatchAttribute.FileNameExtSysRef, CompareFileNameExtsysref()}
            };
        }

        private bool CompareIdentifier(string identifier, bool multipleValuesAllowed = false)
        {
            return CompareIdentifiers(_td1.Identifiers, _td2.Identifiers, identifier, multipleValuesAllowed);
        }

        private bool CompareProductIdentifier(string identifier, bool multipleValuesAllowed = false)
        {
            if (_td1.Product == null || _td2.Product == null)
                return false;

            return CompareIdentifiers(_td1.Product.Identifiers,
                _td2.Product.Identifiers,
                identifier,
                multipleValuesAllowed);
        }

        private InterestedParty[] GetInterestedPartiesByRole(ICollection<InterestedParty> interestedParties, string role)
        {
            var roles = new List<string> { role };

            switch (role)
            {
                case "composer":
                    roles.Add("composer_lyricist");

                    if (_matchConditions.ComposerRoles != null)
                        roles.AddRange(_matchConditions.ComposerRoles);

                    break;
            }

            return interestedParties.Where(ip => roles.Contains(ip.Role))
                                    .OrderBy(ip => ip.Role == role ? 0 : 1)
                                    .Take(6)
                                    .ToArray();
        }

        private float CompareInterestedParties(string role)
        {
            if (_td1.InterestedParties == null || _td2.InterestedParties == null)
                return 0;

            var i1 = GetInterestedPartiesByRole(_td1.InterestedParties, role);
            var i2 = GetInterestedPartiesByRole(_td2.InterestedParties, role);

            var maxIps = Math.Max(i1.Length, i2.Length);

            if (maxIps == 0)
                return 0;

            var scoreMatrix = new Matrix(maxIps);

            // populate the score matrix
            for (int i = 0; i < i1.Length; i++)
            {
                for (int j = 0; j < i2.Length; j++)
                {
                    scoreMatrix[i, j] = CompareInterestedParties(i1[i], i2[j]);
                }
            }

            float topScore = 0;

            // get the highest score
            foreach (var p in scoreMatrix.GetAllPermutations())
            {
                float score = 0;

                for (int i = 0; i < scoreMatrix.Size; i++)
                {
                    score += scoreMatrix[i, p[i]];
                }

                topScore = Math.Max(topScore, score);
            }

            return topScore / scoreMatrix.Size;
        }

        private float CompareInterestedParties(InterestedParty a, InterestedParty b)
        {
            if (a == null || b == null)
                return 0f;

            // 1. ipi
            if (CompareIdentifiers(a.IpIdentifiers, b.IpIdentifiers, "ipi", false))
                return 1f;

            // 2. name-parts
            if (new[] { "composer", "composer_lyricist", "performer" }.Contains(a.Role) &&
                NameCompare.Compare(InterestedPartyExtensions.GenerateNameParts(a), InterestedPartyExtensions.GenerateNameParts(b)))
            {
                return 1f;
            }

            // 3. edit distance
            return DamerauLevenshtein.ComputePercentage(
                a.FullName.Replace(".", " "),
                b.FullName.Replace(".", " "));
        }

        private float CompareProductTitle()
        {
            if (_td1.Product == null || _td2.Product == null)
                return 0;

            return DamerauLevenshtein.ComputePercentage(_td1.Product.Name, _td2.Product.Name);
        }

        private string NormaliseTitle(string value)
        {
            var regexes = new[]
            {
                @"\((.*?)\)",                
                @"- Bonus$",
                @"- Edit$",
                @"- From.*$",
                @"- Original Mix$",
                @"- Remix$",
                @"\d{4} Digital Remaster$",
                @"\d{4} Remaster$"
            }.Select(x => new Regex(x, RegexOptions.IgnoreCase));

            foreach (var re in regexes)
            {
                var match = re.Match(value);

                if (!match.Success)
                {
                    continue;
                }

                var matchValue = match.Groups[0].Value;

                var newTitle = value.Replace(matchValue, "").Trim();

                if (newTitle.Length > 0)
                {
                    return RemoveSpecialCharactorsSpacesAndSomeText(newTitle);
                }
            }

            return RemoveSpecialCharactorsSpacesAndSomeText(value);
        }

        private string RemoveSpecialCharactorsSpacesAndSomeText(string val)
        {
            if (!string.IsNullOrEmpty(val))
            {
                string[] _valList = val.ToLower().Split(' ');
                val = "";
                List<string> _ignoreWords = new List<string>();
                _ignoreWords.Add("a");
                _ignoreWords.Add("&");
                _ignoreWords.Add("an");
                _ignoreWords.Add("the");

                foreach (var item in _valList)
                {
                    if (!_ignoreWords.Contains(item))
                        val += item + " ";
                }

                val = val.Replace("'ve", " ").Replace("'s", " ").Replace("'re", " ").Replace("'t", " ").Replace("'m", " ");
                val = Regex.Replace(val, @"[^\w]+", " ");

                return Regex.Replace(val, " {2,}", " ");
            }
            else
            {
                return "";
            }
        }

        private float CompareTitle()
        {
            var normTitle1 = NormaliseTitle(_td1.Title);
            var normTitle2 = NormaliseTitle(_td2.Title);

            return new[]
            {
                DamerauLevenshtein.ComputePercentage(normTitle1, normTitle2),
                DamerauLevenshtein.ComputePercentage(_td1.Title, _td2.Title),
                DamerauLevenshtein.ComputePercentage(_td1.Title, _td2.AlternativeTitle),
                DamerauLevenshtein.ComputePercentage(_td1.AlternativeTitle, _td2.Title),
                DamerauLevenshtein.ComputePercentage(_td1.AlternativeTitle, _td2.AlternativeTitle)
            }.Max();
        }

        private float CompareFileNameExtsysref()
        {
            bool Match(TrackData t1, TrackData t2)
            {
                return !String.IsNullOrWhiteSpace(t1.FileName) &&
                       (t2?.Identifiers?.ContainsKey("extsysref") ?? false) &&
                       Path.GetFileNameWithoutExtension(t1.FileName).ToLower() ==
                       (t2.Identifiers["extsysref"].ToLower() ?? String.Empty);
            }

            return Match(_td1, _td2) || Match(_td2, _td1)
                ? 1
                : 0;
        }

        /// <summary>
        /// If the matched track contains a product, then one of the product attributes must match.
        /// We'll still be able to match against the product-less version of the work or recording.
        /// </summary>
        private bool IsProductMatch()
        {
            if (MatchTrack.TrackData.Product == null)
                return true;

            // the cat no OR product title must match
            var catNoScore = AttributeScores[MatchAttribute.CatNo];
            var titleScore = AttributeScores[MatchAttribute.ProductTitle];

            return catNoScore >= 1 || titleScore >= 1;
        }

        private bool IdentifierExists(string identifier)
        {
            string val1;
            string val2;

            return _td1.Identifiers != null &&
                   _td2.Identifiers != null &&
                   _td1.Identifiers.TryGetValue(identifier, out val1) &&
                   _td2.Identifiers.TryGetValue(identifier, out val2) &&
                   !string.IsNullOrWhiteSpace(CleanseIdentifier(val1)) &&
                   !string.IsNullOrWhiteSpace(CleanseIdentifier(val2));
        }


        public static TrackCompare Match(Track t1,
                                         Track t2,
                                         MatchConditions matchConditions)
        {
            TrackCompare compare;
            TryMatch(t1, t2, matchConditions, out compare);

            return compare;
        }

        public static bool TryMatch(Track t1, Track t2, MatchConditions matchConditions, out TrackCompare compare)
        {
            compare = new TrackCompare(t1, t2, matchConditions);
            compare.CalculatePercentages();

            return compare.AssignMatch();
        }


        private static bool CompareIdentifiers(IDictionary<string, string> a,
                                               IDictionary<string, string> b,
                                               string identifier,
                                               bool multipleValuesAllowed)
        {
            if (a == null || b == null)
                return false;

            string[] idA;
            string[] idB;

            if (multipleValuesAllowed)
            {
                a.TryGetIdentifiers(identifier, out idA);
                b.TryGetIdentifiers(identifier, out idB);
            }
            else
            {
                string value;

                idA = a.TryGetIdentifier(identifier, out value) ? new[] { value } : new string[0];
                idB = b.TryGetIdentifier(identifier, out value) ? new[] { value } : new string[0];
            }

            idA = idA.Select(CleanseIdentifier).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();
            idB = idB.Select(CleanseIdentifier).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToArray();

            return idA.Any(aa => idB.Any(bb => string.Equals(aa, bb, StringComparison.OrdinalIgnoreCase)));
        }

        private static string CleanseIdentifier(string original)
        {
            return original?.Replace("-", "")
                            .Replace(".", "")
                            .Replace(" ", "");
        }
    }
}
