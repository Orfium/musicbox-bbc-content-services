using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Soundmouse.Utils.DataStructures;

namespace Soundmouse.Utils.Utilities
{
    /// <summary>
    /// Class containing various utilities for <see cref="TimeSpan"/>.
    /// </summary>
    public static class TimespanUtilities
    {
        /// <summary>
        /// Computes the overlap between two ranges of <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="r0">First range.</param>
        /// <param name="r1">Second range.</param>
        /// <returns>Returns the overlap, if any, between the given ranges of <see cref="TimeSpan"/>.</returns>
        public static TimeSpan Overlap(Range<TimeSpan> r0, Range<TimeSpan> r1)
        {
            TimeSpan max = Max(r0.Start, r1.Start);
            TimeSpan min = Min(r0.End, r1.End);
            TimeSpan overlap = min - max;
            
            return overlap.Ticks > 0 ? overlap : TimeSpan.Zero;
        }

        /// <summary>
        /// Determines the largest of two <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="v0">First <see cref="TimeSpan"/>.</param>
        /// <param name="v1">Second <see cref="TimeSpan"/>.</param>
        /// <returns>Returns the largest <see cref="TimeSpan"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan Max(in TimeSpan v0, in TimeSpan v1) => v0 >= v1 ? v0 : v1;

        /// <summary>
        /// Determines the smallest of two <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="v0">First <see cref="TimeSpan"/>.</param>
        /// <param name="v1">Second <see cref="TimeSpan"/>.</param>
        /// <returns>Returns the smallest <see cref="TimeSpan"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan Min(in TimeSpan v0, in TimeSpan v1) => v0 <= v1 ? v0 : v1;

        /// <summary>
        /// Sums all <see cref="TimeSpan"/> in the collection.
        /// </summary>
        /// <param name="input">Collection of <see cref="TimeSpan"/>.</param>
        /// <returns>Returns a <see cref="TimeSpan"/> that represents the sum of all <see cref="TimeSpan"/> in the collection.</returns>
        public static TimeSpan SumTimespan(IEnumerable<TimeSpan> input) => input == null ? TimeSpan.Zero : TimeSpan.FromTicks(input.Sum(i => i.Ticks));

        /// <summary>
        /// Sums all <see cref="TimeSpan" /> in the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input">Collection of items..</param>
        /// <param name="selector">Selector used to pick the <see cref="TimeSpan"/> to sum.</param>
        /// <returns>Returns a <see cref="TimeSpan" /> that represents the sum of all <see cref="TimeSpan" /> in the collection.</returns>
        /// <exception cref="ArgumentNullException">input</exception>
        /// <exception cref="ArgumentNullException">selector</exception>
        public static TimeSpan SumTimespan<T>(IEnumerable<T> input, Func<T, TimeSpan> selector)
        {
            if(input == null)
                throw new ArgumentNullException(nameof(input));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            return SumTimespan(input.Select(selector));
        }
    }
}
