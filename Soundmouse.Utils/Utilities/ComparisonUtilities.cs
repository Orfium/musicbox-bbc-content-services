using System;
using System.Collections.Generic;
using System.Linq;

namespace Soundmouse.Utils.Utilities
{
    /// <summary>
    /// Class containg comparison utilities.
    /// </summary>
    public static class ComparisonUtilities
    {
        /// <summary>
        /// Checks if the given arrays are equal.
        /// </summary>
        /// <typeparam name="T">Type in the array</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <returns>Returns <c>true</c> if both arrays are the same; Otherwise, it returns <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">equalityComparer</exception>
        /// <remarks>
        /// This method does a stric order comparison. It does not attempt to order the arrays nor does it
        /// check if the arrays are equivalent (i.e. same elements but in different order).
        /// </remarks>
        public static bool Equals<T>(T[] x, T[] y) => Equals(x, y, EqualityComparer<T>.Default);

        /// <summary>
        /// Checks if the given arrays are equal.
        /// </summary>
        /// <typeparam name="T">Type in the array</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <param name="equalityComparer">Equality comparer of <typeparamref name="T"/>.</param>
        /// <returns>Returns <c>true</c> if both arrays are the same; Otherwise, it returns <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">equalityComparer</exception>
        /// <remarks>
        /// This method does a stric order comparison. It does not attempt to order the arrays nor does it
        /// check if the arrays are equivalent (i.e. same elements but in different order).
        /// </remarks>
        public static bool Equals<T>(T[] x, T[] y, IEqualityComparer<T> equalityComparer)
        {
            if (equalityComparer == null)
                throw new ArgumentNullException(nameof(equalityComparer));

            bool xIsNull = x == null;
            bool yIsNull = y == null;

            if (xIsNull && yIsNull)
                return true;

            if (xIsNull ^ yIsNull)
                return false;

            if (ReferenceEquals(x, y))
                return true;

            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (!equalityComparer.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the given arrays are equivalent.
        /// </summary>
        /// <typeparam name="T">Type in the array</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <returns>Returns <c>true</c> if both arrays have the same elements; Otherwise, it returns <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">equalityComparer</exception>
        /// <remarks>
        /// This method will check if all the elements are the same no matter the order they are in.
        /// </remarks>
        public static bool AreEquivalent<T>(T[] x, T[] y) => AreEquivalent(x, y, EqualityComparer<T>.Default);

        /// <summary>
        /// Checks if the given arrays are equal.
        /// </summary>
        /// <typeparam name="T">Type in the array</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <param name="equalityComparer">Equality comparer of <typeparamref name="T"/>.</param>
        /// <returns>Returns <c>true</c> if both arrays have the same elements; Otherwise, it returns <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">equalityComparer</exception>
        /// <remarks>
        /// This method will check if all the elements are the same no matter the order they are in.
        /// </remarks>
        public static bool AreEquivalent<T>(T[] x, T[] y, IEqualityComparer<T> equalityComparer)
        {
            if (equalityComparer == null)
                throw new ArgumentNullException(nameof(equalityComparer));

            bool xIsNull = x == null;
            bool yIsNull = y == null;

            if (xIsNull && yIsNull)
                return true;

            if (xIsNull ^ yIsNull)
                return false;

            if (ReferenceEquals(x, y))
                return true;

            if (x.Length != y.Length)
                return false;

            // Creating copies of the arrays so that sorting them won't affect the actual's array order of the elements
            T[] xCopy = (T[])x.Clone();
            T[] yCopy = (T[])y.Clone();
            Array.Sort(xCopy);
            Array.Sort(yCopy);

            for (int i = 0; i < xCopy.Length; i++)
            {
                if (!equalityComparer.Equals(xCopy[i], yCopy[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if given dictionaries are equal.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <returns>Returns <c>true</c> if both dictionaries are the same; Otherwise, it returns <c>false</c>.</returns>
        /// <remarks>
        /// This method does a stric order comparison. It does not attempt to order the dictionaries nor does it
        /// check if the dictionaries are equivalent (i.e. same elements but in different order).
        /// </remarks>
        public static bool Equals<TKey, TValue>(Dictionary<TKey, TValue> x,
                                                Dictionary<TKey, TValue> y) => Equals(x,
                                                                                      y,
                                                                                      EqualityComparer<TKey>.Default,
                                                                                      EqualityComparer<TValue>.Default);

        /// <summary>
        /// Checks if given dictionaries are equal.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <param name="keyEqualityComparer">Key equality comparer.</param>
        /// <param name="valueEqualityComparer">Value equality comparer.</param>
        /// <returns>Returns <c>true</c> if both dictionaries are the same; Otherwise, it returns <c>false</c>.</returns>
        /// <remarks>
        /// This method does a stric order comparison. It does not attempt to order the dictionaries nor does it
        /// check if the dictionaries are equivalent (i.e. same elements but in different order).
        /// </remarks>
        public static bool Equals<TKey, TValue>(Dictionary<TKey, TValue> x,
                                                Dictionary<TKey, TValue> y,
                                                IEqualityComparer<TKey> keyEqualityComparer,
                                                IEqualityComparer<TValue> valueEqualityComparer)
        {
            if(keyEqualityComparer == null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            if(valueEqualityComparer == null)
                throw new ArgumentNullException(nameof(valueEqualityComparer));

            bool xIsNull = x == null;
            bool yIsNull = y == null;

            if (xIsNull && yIsNull)
                return true;

            if (xIsNull ^ yIsNull)
                return false;

            if (ReferenceEquals(x, y))
                return true;

            if (x.Count != y.Count)
                return false;

            if (!Equals(x.Keys.ToArray(), y.Keys.ToArray(), keyEqualityComparer))
                return false;

            foreach (var kv in x)
            {
                if (!valueEqualityComparer.Equals(kv.Value, y[kv.Key]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the given arrays are equal.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <returns>Returns <c>true</c> if both dictionaries have the same elements; Otherwise, it returns <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">equalityComparer</exception>
        /// <remarks>
        /// This method will check if all the elements are the same no matter the order they are in.
        /// </remarks>
        public static bool AreEquivalent<TKey, TValue>(Dictionary<TKey, TValue> x,
                                                Dictionary<TKey, TValue> y) => AreEquivalent(x,
                                                                                      y,
                                                                                      EqualityComparer<TKey>.Default,
                                                                                      EqualityComparer<TValue>.Default);

        /// <summary>
        /// Checks if the given arrays are equal.
        /// </summary>
        /// <typeparam name="TKey">Type of the key.</typeparam>
        /// <typeparam name="TValue">Type of the value.</typeparam>
        /// <param name="x">X.</param>
        /// <param name="y">Y.</param>
        /// <param name="keyEqualityComparer">Key equality comparer.</param>
        /// <param name="valueEqualityComparer">Value equality comparer.</param>
        /// <returns>Returns <c>true</c> if both dictionaries have the same elements; Otherwise, it returns <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">equalityComparer</exception>
        /// <remarks>
        /// This method will check if all the elements are the same no matter the order they are in.
        /// </remarks>
        public static bool AreEquivalent<TKey, TValue>(Dictionary<TKey, TValue> x,
                                                Dictionary<TKey, TValue> y,
                                                IEqualityComparer<TKey> keyEqualityComparer,
                                                IEqualityComparer<TValue> valueEqualityComparer)
        {
            if (keyEqualityComparer == null)
                throw new ArgumentNullException(nameof(keyEqualityComparer));

            if (valueEqualityComparer == null)
                throw new ArgumentNullException(nameof(valueEqualityComparer));

            bool xIsNull = x == null;
            bool yIsNull = y == null;

            if (xIsNull && yIsNull)
                return true;

            if (xIsNull ^ yIsNull)
                return false;

            if (ReferenceEquals(x, y))
                return true;

            if (x.Count != y.Count) 
                return false;

            if (!AreEquivalent(x.Keys.ToArray(), y.Keys.ToArray(), keyEqualityComparer))
                return false;

            foreach (var kvp in x)
            {
                if (!valueEqualityComparer.Equals(kvp.Value, y[kvp.Key])) return false;
            }

            return true;
        }
    }
}
