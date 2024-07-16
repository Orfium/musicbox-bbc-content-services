using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Serilog;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Contains extensions for Serilog.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class SerilogExtensions
    {
        /// <summary>
        /// Determines whether Verbose is enabled for the given <see cref="ILogger"/>.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>Returns <c>true</c> if Verbose is enabled for the given <see cref="ILogger"/>; otherwise, returns <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVerboseEnabled(this ILogger logger) => logger.IsEnabled(Serilog.Events.LogEventLevel.Verbose);

        /// <summary>
        /// Determines whether Debug is enabled for the specified logger.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>Returns <c>true</c> if Debug is enabled for the given <see cref="ILogger"/>; otherwise, returns <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDebugEnabled(this ILogger logger) => logger.IsEnabled(Serilog.Events.LogEventLevel.Debug);

        /// <summary>
        /// Determines whether Information is enabled for the specified logger.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>Returns <c>true</c> if Information is enabled for the given <see cref="ILogger"/>; otherwise, returns <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInformationEnabled(this ILogger logger) => logger.IsEnabled(Serilog.Events.LogEventLevel.Information);

        /// <summary>
        /// Determines whether Warning is enabled for the specified logger.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>Returns <c>true</c> if Warning is enabled for the given <see cref="ILogger"/>; otherwise, returns <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWarningEnabled(this ILogger logger) => logger.IsEnabled(Serilog.Events.LogEventLevel.Warning);

        /// <summary>
        /// Determines whether Error is enabled for the specified logger.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>Returns <c>true</c> if Error is enabled for the given <see cref="ILogger"/>; otherwise, returns <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsErrorEnabled(this ILogger logger) => logger.IsEnabled(Serilog.Events.LogEventLevel.Error);

        /// <summary>
        /// Determines whether Fatal is enabled for the specified logger.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>Returns <c>true</c> if Fatal is enabled for the given <see cref="ILogger"/>; otherwise, returns <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFatalEnabled(this ILogger logger) => logger.IsEnabled(Serilog.Events.LogEventLevel.Fatal);
    }
}