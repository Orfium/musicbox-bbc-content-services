using MusicManager.PrsSearch.DataMatching.Util;
using Soundmouse.Messaging.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching
{
    public class NameCompare
    {
        public static bool Compare(ICollection<Name> a, ICollection<Name> b)
        {
            if (a == null || a.Count == 0 || b == null || b.Count == 0)
                return false;

            return CompareNames(a, b, NamePart.First) &&
                   CompareNames(a, b, NamePart.Middle) &&
                   CompareNames(a, b, NamePart.Last);
        }

        /// <summary>
        /// Compare all names of the specified name-part.
        /// </summary>
        private static bool CompareNames(IEnumerable<Name> a, IEnumerable<Name> b, NamePart namePart)
        {
            var aa = a.Where(n => n.NamePart == namePart).Select(n => AsciiFoldingFilter.Fold(n.Value)).ToArray();
            var bb = b.Where(n => n.NamePart == namePart).Select(n => AsciiFoldingFilter.Fold(n.Value)).ToArray();

            var score = CompareNames(aa, bb, namePart);

            if (score.HasValue)
                return score.Value;

            // if unable to match middle, default to match
            return namePart == NamePart.Middle;
        }

        private static bool? CompareNames(string[] a, string[] b, NamePart namePart)
        {
            // compare the names in the order given
            var length = Math.Min(a.Length, b.Length);

            if (length == 0)
            {
                // score to depend on name part: only last name is required
                return namePart != NamePart.Last;
            }

            // is the # of names for each party the same? if not, auto non-match, unless comparing middle names
            if (length != Math.Max(a.Length, b.Length) && namePart != NamePart.Middle)
            {
                return false;
            }


            var canMatchInitial = namePart != NamePart.Last;

            for (var i = 0; i < length; i++)
            {
                if (!IsWordMatch(a[i], b[i], canMatchInitial))
                    return false;
            }

            return true;
        }

        private static bool IsWordMatch(string a, string b, bool canMatchInitial = true)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;

            if (a == b)
                return true;

            if (!canMatchInitial)
                return false;

            if (a.Length == 1)
                return a == b.Substring(0, 1);

            if (b.Length == 1)
                return b == a.Substring(0, 1);

            return false;
        }
    }
}
