using System;
using System.Collections.Generic;

using Soundmouse.Utils.Utilities;

namespace Soundmouse.Utils
{
    /// <summary>
    /// Class that wraps a number of <see cref="IDisposable"/> instances and will dispose of them
    /// once the instance is disposed.
    /// Implements the <see cref="System.IDisposable" />
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <remarks>
    /// The given <see cref="IDisposable"/> will be disposed in the same order as they are supplied in the constructor.
    /// </remarks>
    public sealed class CompositeDisposable : IDisposable
    {
        #region Private fields

        private bool _disposed;
        private readonly List<IDisposable> _disposables;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeDisposable" /> class.
        /// </summary>
        public CompositeDisposable(params IDisposable[] disposables)
        {
            _disposables = new List<IDisposable>(disposables);
        }

        #endregion

        #region IDisposable implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var disposable in _disposables)
            {
                DelegateUtilities.Swallow(disposable.Dispose);
            }

            _disposables.Clear();

            _disposed = true;
        }

        #endregion
    }
}
