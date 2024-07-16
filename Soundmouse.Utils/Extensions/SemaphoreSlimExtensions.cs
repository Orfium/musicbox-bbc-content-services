using System;
using System.Threading;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Class containing extensions for <see cref="SemaphoreSlim"/>.
    /// </summary>
    public static class SemaphoreSlimExtensions
    {
        /// <summary>
        /// Enters the specified semaphore.
        /// </summary>
        /// <param name="semaphore">The semaphore.</param>
        /// <returns>IDisposable.</returns>
        public static IDisposable Enter(this SemaphoreSlim semaphore)
        {
            if(semaphore == null)
                throw new ArgumentNullException(nameof(semaphore));

            semaphore.Wait();

            return new Disposable(() => semaphore.Release());
        }
    }
}
