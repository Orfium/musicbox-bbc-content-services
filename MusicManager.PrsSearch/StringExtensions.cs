using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Soundmouse.Matching
{
    public static class StringExtensions
    {
        public static string Truncate(this string value, int length)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            value = value.Trim();

            return value.Length <= length
                ? value
                : value.Substring(0, length).Trim();
        }

        /// <summary>
        /// Truncate the string, but if possible do not break mid-word.
        /// </summary>
        public static string TruncateWords(this string value, int length)
        {
            if (value.Length <= length)
                return value;

            var words = value.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder();

            foreach (var w in words)
            {
                if (sb.Length + w.Length + (sb.Length > 0 ? 1 : 0) <= length)
                {
                    if (sb.Length > 0)
                        sb.Append(' ');

                    sb.Append(w);
                }
                else
                {
                    break;
                }
            }

            if (sb.Length == 0)
                return value.Truncate(length);

            return sb.ToString();
        }
    }
}