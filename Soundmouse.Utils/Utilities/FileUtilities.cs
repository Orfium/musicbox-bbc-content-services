using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Soundmouse.Utils.Utilities
{
    /// <summary>
    /// Class containing various utilities to use when dealing with files.
    /// </summary>
    public static class FileUtilities
    {
        #region Private fields

        private static readonly char[] _windowsDisallowedChars = {'<', '>', ':', '"', '/', '\\', '|', '?', '*', '\t'};

        #endregion

        #region Public methods

        /// <summary>
        /// Sanitises the given input.
        /// </summary>
        /// <param name="input">Input to sanitise.</param>
        /// <returns>Returns a sanitised <see cref="string"/> that can be used as a filename on Windows.</returns>
        public static string Sanitise(string input)
        {
            static string ProcessSplitEntries(string s) => s;

            return Sanitise(input, ProcessSplitEntries);
        }

        /// <summary>
        /// Sanitises the given input and converts it to TitleCase with the given <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="input">Input to sanitise.</param>
        /// <param name="cultureInfo">Culture information to use when sanitising the input.</param>
        /// <returns>Returns a sanitised <see cref="string" /> that can be used as a filename on Windows.</returns>
        /// <exception cref="System.ArgumentNullException">cultureInfo</exception>
        public static string Sanitise(string input, CultureInfo cultureInfo)
        {
            if (cultureInfo == null)
                throw new ArgumentNullException(nameof(cultureInfo));
            
            return Sanitise(input, cultureInfo.TextInfo.ToTitleCase);
        }

        #endregion

        #region Private fields

        private static string Sanitise(string input, Func<string, string> processSplitEntries)
        {
            Debug.Assert(processSplitEntries != null);

            return string.IsNullOrWhiteSpace(input)
                       ? string.Empty
                       : string.Join("-", input.Split(_windowsDisallowedChars, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(processSplitEntries))
                               .Replace(" ", string.Empty);
        }

        #endregion
    }
}
