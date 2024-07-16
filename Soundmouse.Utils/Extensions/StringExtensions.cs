using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Soundmouse.Utils.Utilities;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Class containing various extensions for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        private const string SnakeCaseSeparator = "_";
        private static readonly string[] _snakeCaseSeparatorArray = {SnakeCaseSeparator};
        private static readonly Regex _newlineRegex = new Regex("(\r\n|\r|\n)+", RegexOptions.Compiled);
        private static readonly Regex _duplicateRegex = new Regex("(?<space> ) +|(?<tab>\t)\t+", RegexOptions.Compiled);

        #region Public methods

        /// <summary>
        /// Converts the given string to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">String to convert.</param>
        /// <returns>Returns a stream containing the converted string.</returns>
        /// <remarks>
        /// String is encoded in UTF-8.
        /// </remarks>
        public static Stream ToStream(this string input) => ToStream(input, Encoding.UTF8);

        /// <summary>
        /// Converts the given string to a <see cref="Stream" />.
        /// </summary>
        /// <param name="input">String to convert.</param>
        /// <param name="encoding">Encoding to use when converting the string.</param>
        /// <returns>Returns a stream containing the converted string.</returns>
        public static Stream ToStream(this string input, Encoding encoding) => new MemoryStream(encoding.GetBytes(input ?? string.Empty));

        /// <summary>
        /// Converts a string to snake_case.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the input in snake_case format.</returns>
        public static string ToSnakeCase(this string input) => SeparateWords(input, SnakeCaseSeparator);

        /// <summary>
        /// Convert an object to camelCase using its type.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the input in camelCase format.</returns>
        public static string ToCamelCase(this object input)
        {
            if (input is string s)
                return s.ToCamelCase();

            return ToCamelCase(input.GetType().Name);
        }

        /// <summary>
        /// Convert a string to camelCase.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the input in camelCase format.</returns>
        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input) || char.IsLower(input[0]))
                return input;

            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }
        
        /// <summary>
        /// Converts a snake_case input to camelCase.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the converted input.</returns>
        public static string FromSnakeCaseToCamelCase(this string input) => ToCamelCase(FromSnakeCaseToPascalCase(input));

        /// <summary>
        /// Converts a string to PascalCase.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the input in PascalCase format.</returns>
        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input) || char.IsUpper(input[0]))
                return input;

            return char.ToUpperInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Converts a snake_case input to camelCase.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the converted input.</returns>
        public static string FromSnakeCaseToPascalCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Split(_snakeCaseSeparatorArray,
                               StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
                        .Aggregate(string.Empty, (s1, s2) => s1  + s2);
        }
        
        /// <summary>
        /// Removes all new lines from the input.
        /// </summary>
        /// <param name="input">The value.</param>
        /// <returns>Returns the input without new lines.</returns>
        public static string RemoveNewLine(this string input)
        {
            return string.IsNullOrWhiteSpace(input) ? input : _newlineRegex.Replace(input, " ");
        }

        /// <summary>
        /// Compacts the whitespace found in the input.
        /// </summary>
        /// <param name="input">Input to compact.</param>
        /// <returns>Returns the compacted input.</returns>
        public static string CompactWhitespace(this string input)
        {
            static string EvaluateMatch(Match match)
            {
                Debug.Assert(match.Success);

                return match.Groups["space"].Value == string.Empty 
                           ? match.Groups["tab"].Value 
                           : match.Groups["space"].Value;
            }

            return string.IsNullOrEmpty(input) 
                       ? input 
                       : _duplicateRegex.Replace(input, EvaluateMatch);
        }

        /// <summary>
        /// Converts the given input into a string array.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>System.String[].</returns>
        public static string[] AsStringArray(this string? input) => input == null ? new string[0] : new[] {input};

        /// <summary>
        /// Safely gets a substring from the given input.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <param name="from">Start position of the substring.</param>
        /// <param name="length">Length of the start position.</param>
        /// <returns>System.String.</returns>
        public static string SafeSubstring(this string input, int from, int? length = null)
        {
            from = Math.Abs(from);

            if (string.IsNullOrEmpty(input) || input.Length <= from)
                return string.Empty;

            return length == null
                       ? input.Substring(from)
                       : input.Substring(from, Math.Min(input.Length - from, Math.Abs(length.Value)));
        }

        /// <summary>
        /// Truncates the given input to the specified length.
        /// </summary>
        /// <param name="input">Input to truncat.</param>
        /// <param name="length">Input's max length.</param>
        /// <returns>Returns the truncated input.</returns>
        public static string Truncate(this string input, int length)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            length = Math.Abs(length);

            return input.Length > length 
                       ? input.Substring(0, length) 
                       : input;
        }

        /// <summary>
        /// Tries to parse the given input as a nullable GUID.
        /// </summary>
        /// <param name="input">Input to parse.</param>
        /// <returns>Returns a new <see cref="Nullable{Guid}"/>.</returns>
        public static Guid? TryParseAsGuid(this string input) => Guid.TryParse(input, out Guid guid) ? (Guid?) guid : null;

        /// <summary>
        /// Gets the MD5 hash of the input.
        /// </summary>
        /// <param name="input">Input to hash.</param>
        /// <returns>Returns the lowercased MD5 hash of the input.</returns>
        public static string ToMd5Hash(this string input)
        {
            using var strStream = input.ToStream();

            return strStream.CalculateMd5();
        }

        #endregion

        #region Private methods

        private static string SeparateWords(string input, 
                                            string delimiter)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var sb = new StringBuilder();

            foreach (var c in input)
            {
                if (char.IsUpper(c) && sb.Length > 0)
                {
                    sb.Append(delimiter);
                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().ToLower();
        }

        #endregion
    }
}