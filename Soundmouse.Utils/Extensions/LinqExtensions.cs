using System;
using System.Collections.Generic;
using System.Linq;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Class containing various extensions for LINQ.
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Splits the given <see cref="IEnumerable{T}"/> into <see cref="List{T}"/> of the given <paramref name="batchSize"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="input">Input to split.</param>
        /// <param name="batchSize">Size of the lists to be created from the given <see cref="IEnumerable{T}"/>.</param>
        /// <returns>IEnumerable&lt;List&lt;T&gt;&gt;.</returns>
        /// <remarks>
        /// Borrowed from: https://stackoverflow.com/a/11463800
        /// </remarks>
        public static IEnumerable<List<T>> Split<T>(this IEnumerable<T> input, int batchSize)
        {
            batchSize = Math.Abs(batchSize);

            var materialisedInput = input?.ToList() ?? new List<T>();
            for (int i = 0; i < materialisedInput.Count; i += batchSize)
            {
                yield return materialisedInput.GetRange(i, Math.Min(batchSize, materialisedInput.Count - i));
            }
        }

        /// <summary>
        /// Converts <see cref="IEnumerable{T}"/> into <see cref="List{T}"/> of max size <paramref name="blockSize"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">Input <see cref="IEnumerable{T}"/></param>
        /// <param name="blockSize">Size of the lists to be created from the given <see cref="IEnumerable{T}"/>.</param>
        /// <returns>IEnumerable&lt;List&lt;T&gt;&gt;.</returns>
        public static IEnumerable<List<T>> ReadBlock<T>(this IEnumerable<T> source, int blockSize)
        {
            List<T> currentBlock = new List<T>(blockSize);
            foreach (var entry in source)
            {
                currentBlock.Add(entry);

                if (currentBlock.Count >= blockSize)
                {
                    yield return currentBlock;

                    currentBlock.Clear();
                }
            }

            if (currentBlock.Any()) yield return currentBlock;
        }
    }
}
