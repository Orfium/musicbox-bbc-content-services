using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicManager.PrsSearch.DataMatching
{
    public static class IdentifierExtensions
    {
        public static bool TryGetIdentifier(this IDictionary<string, string> identifiers,
                                            string identifier,
                                            out string value)
        {
            if (identifiers == null)
            {
                value = null;
                return false;
            }

            return identifiers.TryGetValue(identifier, out value);
        }

        public static bool TryGetIdentifiers(this IDictionary<string, string> identifiers,
                                             string identifierPrefix,
                                             out string[] value)
        {
            if (identifiers == null)
            {
                value = new string[0];
                return false;
            }

            value = identifiers.Keys.Where(k => k.StartsWith(identifierPrefix, StringComparison.OrdinalIgnoreCase))
                               .Select(k => identifiers[k])
                               .ToArray();

            return value.Any();
        }
    }
}
