using System;
using System.Collections.Generic;
using System.Text;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Class containing extensions for <see cref="TimeSpan"/>.
    /// </summary>
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Ole Automation epoch.
        /// </summary>
        public static DateTime EpochOA { get; } = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts the given input into a OLE Automation time equivalent.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the amount of seconds passed between UNIX epoch and the input.</returns>        
        /// <value>A double-precision floating-point number that contains an OLE Automation time equivalent.</value>
        public static double ToOATime(this TimeSpan input)
        {              
            return EpochOA.Add(input).ToOADate();
        }
    }
}
