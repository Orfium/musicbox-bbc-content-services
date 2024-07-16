using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Soundmouse.Utils.DataStructures
{
    /// <summary>
    /// Class FastGrouping. This class cannot be inherited.
    /// Implements the <see cref="System.Linq.IGrouping{TKey, TElement}" />
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TElement">The type of the t element.</typeparam>
    /// <seealso cref="System.Linq.IGrouping{TKey, TElement}" />
    public sealed class FastGrouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        #region IGrouping implementation

        /// <inheritdoc />
        public TKey Key { get; }
        
        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        public IEnumerator<TElement> GetEnumerator() => Items.GetEnumerator();

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the items.
        /// </summary>
        public IReadOnlyList<TElement> Items { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Prevents a default instance of the <see cref="FastGrouping{TKey, TElement}" /> class from being created.
        /// </summary>
        /// <param name="key">Group key.</param>
        /// <param name="items">Group items.</param>
        private FastGrouping(TKey key, IReadOnlyList<TElement> items)
        {
            Key = key;
            Items = items;
        }

        #endregion
        
        #region Public static methods

        /// <summary>
        /// Fast groups the given collection.
        /// </summary>
        /// <param name="source">Source to fast group.</param>
        /// <param name="keySelector">Key selector function.</param>
        /// <returns>Returns the source collection fast grouped.</returns>
        public static FastGrouping<TKey, TElement>[] GroupBy(IEnumerable<TElement> source, 
                                                             Func<TElement, TKey> keySelector)
        {
            static TElement Transform(TElement element) => element;

            return GroupBy(source, keySelector, Transform);
        }

        /// <summary>
        /// Fast groups the given collection.
        /// </summary>
        /// <typeparam name="TSource">Type of the source.</typeparam>
        /// <param name="source">Source to fast group.</param>
        /// <param name="keySelector">Key selector function.</param>
        /// <param name="elementSelector">Element selector function.</param>
        /// <returns>Returns the source collection fast grouped.</returns>
        public static FastGrouping<TKey, TElement>[] GroupBy<TSource>(IEnumerable<TSource> source,
                                                                      Func<TSource, TKey> keySelector,
                                                                      Func<TSource, TElement> elementSelector)
        {
            if(source == null)
                throw new ArgumentNullException(nameof(source));

            if(keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if(elementSelector == null)
                throw new ArgumentNullException(nameof(elementSelector));

            Dictionary<TKey, List<TElement>> lookup = new Dictionary<TKey, List<TElement>>();
            
            // Contains all elements whose keys were null
            List<TElement> nullKeyElements = new List<TElement>();
            
            foreach (var sourceEntry in source)
            {
                TKey key = keySelector(sourceEntry);
                
                List<TElement> group;
                if (key != null)
                {
                    if (!lookup.TryGetValue(key, out group))
                    {
                        lookup[key] = group = new List<TElement>();
                    }
                }
                else
                {
                    group = nullKeyElements;
                }

                TElement element = elementSelector(sourceEntry);
                group.Add(element);
            }

            FastGrouping<TKey, TElement>[] groups = new FastGrouping<TKey, TElement>[lookup.Count + (nullKeyElements.Count > 0 ? 1 : 0)];
            
            int cursor = 0;

            foreach (KeyValuePair<TKey, List<TElement>> kvp in lookup) 
                groups[cursor++] = new FastGrouping<TKey, TElement>(kvp.Key, kvp.Value);

            if (nullKeyElements.Count > 0) 
                groups[groups.Length - 1] = new FastGrouping<TKey, TElement>(default, nullKeyElements);

            return groups;
        }

        #endregion
    }
}
