using System;
using System.Diagnostics.CodeAnalysis;

namespace Soundmouse.Utils.DataStructures
{
    /// <summary>
    /// Represents a range of values.
    /// </summary>
    /// <typeparam name="T">The underlying type of the values of the range.</typeparam>
    [ExcludeFromCodeCoverage]
    public sealed class Range<T> where T : IComparable<T>
    {
        /// <summary>
        /// Gets or sets the starting value of the range.
        /// </summary>
        /// <value>
        /// The starting value.
        /// </value>
        public T Start { get; }

        /// <summary>
        /// Gets or sets the ending value of the range.
        /// </summary>
        /// <value>
        /// The ending value.
        /// </value>
        public T End { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Range{T}"/> class.
        /// </summary>
        /// <param name="start">The starting value of the range.</param>
        /// <param name="end">The ending value of the range.</param>
        public Range(T start, T end)
        {
            Start = start;
            End = end;
        }
    }
}
