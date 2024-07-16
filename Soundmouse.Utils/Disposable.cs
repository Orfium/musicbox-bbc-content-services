using System;
using System.Diagnostics.CodeAnalysis;

using Soundmouse.Utils.Extensions;
using Soundmouse.Utils.Utilities;

namespace Soundmouse.Utils
{
    /// <summary>
    /// Class that wraps an action that will be invoked when it's disposed.
    /// Implements the <see cref="System.IDisposable" /></summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class Disposable : IDisposable
    {
        #region Private fields

        private readonly Action _onDispose;
        private bool _disposed;
        private readonly string _creator;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Disposable" /> class.
        /// </summary>
        /// <param name="onDispose">Action to call when disposing of this instance.</param>
        public Disposable(Action onDispose)
            : this(onDispose, 1 /*this one*/)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Disposable"/> class.
        /// </summary>
        /// <param name="onDispose">Action to call when disposing of this instance.</param>
        /// <param name="nrOfFrames">Number of frames to skip when getting the creator.</param>
        private Disposable(Action onDispose, int nrOfFrames)
        {
            _onDispose = onDispose;

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (Serilog.Log.Logger.IsVerboseEnabled())
                _creator = MiscUtilities.GetCallingType(nrOfFrames + 1);
            else
                _creator = string.Empty;
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Generates a <see cref="Disposable"/> that, when disposed, invokes an empty action.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static Disposable Empty => new Disposable(() =>
                                                         {
                                                             if (Serilog.Log.Logger.IsVerboseEnabled())
                                                                 Serilog.Log.Verbose("Invoking empty disposable action for '{Caller}'",
                                                                                     MiscUtilities.GetCallingType(2 /*this action and the Dispose method*/));
                                                         },
                                                         1);

        #endregion

        #region IDisposable implementation

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            if (Serilog.Log.Logger.IsVerboseEnabled())
                Serilog.Log.Verbose("Invoking disposable action created by '{Creator}' on behalf of '{Caller}'",
                                    _creator,
                                    MiscUtilities.GetCallingType(1 /*this method*/));

            _onDispose();
            _disposed = true;
        }

        #endregion
    }
}
