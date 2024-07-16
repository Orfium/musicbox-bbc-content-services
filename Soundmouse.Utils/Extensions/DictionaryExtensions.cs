using System.Collections.Generic;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Class containing extensions for <see cref="IDictionary{TKey,TValue}"/>.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets the value associated with the specified key from a dictionary or a default if the key
        /// does not exist in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary object.</param>
        /// <param name="key">The key.</param>
        /// <param name="default">The default value.</param>
        /// <returns>The value in the dictionary or the default value if the key does not exist.</returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
                                                             TKey key,
                                                             TValue @default = default)
        {
            return dictionary.TryGetValue(key, out var value) ? value : @default;
        }
    }
}
