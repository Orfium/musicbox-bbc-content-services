using System;
using System.Threading;

using Serilog.Events;

namespace Soundmouse.Utils.Utilities
{
    /// <summary>
    /// Class containing various utilities for <see cref="Delegate"/>.
    /// </summary>
    public static class DelegateUtilities
    {
        /// <summary>
        /// Repeats the specified action for a specific number of times.
        /// </summary>
        /// <param name="action">Action which has to be repeated in case of failure</param>
        /// <param name="maxRetries">How many times to retry before failing</param>
        /// <param name="wait">Amount of time to wait</param>
        /// <param name="terminalExceptionDelegate">Delegate which returns [true] for exception which has to cancel repeating.</param>
        /// <param name="logLevel">Log level used to write interim failure messages.</param>
        /// <exception cref="ArgumentNullException">action</exception>
        public static void Repeat(Action action,
                                  int maxRetries,
                                  in TimeSpan wait,
                                  Func<Exception, bool> terminalExceptionDelegate = null,
                                  LogEventLevel logLevel = LogEventLevel.Warning)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            int retries = 1;

            while (true)
            {
                try
                {
                    action.Invoke();

                    break;
                }
                catch (Exception exception)
                {
                    // If repeated more than allowed, throw exception
                    if (retries                                         >= maxRetries
                        || terminalExceptionDelegate?.Invoke(exception) == true)
                    {
                        Serilog.Log.Error(exception, "Operation failed after retrying {Retries}", retries);

                        throw;
                    }

                    Serilog.Log.Logger.Write(logLevel,
                                             exception,
                                             "Operation failed {Retries} of {MaxRetries}. Retrying in {Wait}",
                                             retries,
                                             maxRetries,
                                             wait);

                    // Wait for specified period
                    if (wait != default && wait != TimeSpan.Zero)
                        Thread.Sleep(wait);

                    retries += 1;
                }
            }
        }

        /// <summary>
        /// Repeats the specified function for a specific number of times.
        /// </summary>
        /// <param name="func">Function which has to be repeated in case of failure</param>
        /// <param name="maxRetries">How many times to retry before failing</param>
        /// <param name="wait">Amount of time to wait</param>
        /// <param name="terminalExceptionDelegate">Delegate which returns [true] for exception which has to cancel repeating.</param>
        public static TResult Repeat<TResult>(Func<TResult> func,
                                              int maxRetries,
                                              in TimeSpan wait,
                                              Func<Exception, bool> terminalExceptionDelegate = null,
                                              LogEventLevel logLevel = LogEventLevel.Warning)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            int retries = 1;

            while (true)
            {
                try
                {
                    return func.Invoke();
                }
                catch (Exception exception)
                {
                    // If repeated more than allowed, throw exception
                    if (retries                                         >= maxRetries
                        || terminalExceptionDelegate?.Invoke(exception) == true)
                    {
                        Serilog.Log.Error(exception, "Operation failed after retrying {Retries}", retries);

                        throw;
                    }

                    Serilog.Log.Logger.Write(logLevel,
                                             "Operation failed {Retries} of {MaxRetries}. Retrying in {Wait}",
                                             retries,
                                             maxRetries,
                                             wait);

                    // Wait for specified period
                    if (wait != default && wait != TimeSpan.Zero)
                        Thread.Sleep(wait);

                    retries += 1;
                }
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action"/>. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel"/> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <param name="action">Action to invoke.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow(Action action, LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1>(Action<T1> action,
                                       T1 arg1,
                                       LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <typeparam name="T2">Type of argument 2.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1, T2>(Action<T1, T2> action,
                                           T1 arg1,
                                           T2 arg2,
                                           LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1, arg2);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <typeparam name="T2">Type of argument 2.</typeparam>
        /// <typeparam name="T3">Type of argument 3.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <param name="arg3">Argument 3.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1, T2, T3>(Action<T1, T2, T3> action,
                                               T1 arg1,
                                               T2 arg2,
                                               T3 arg3,
                                               LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <typeparam name="T2">Type of argument 2.</typeparam>
        /// <typeparam name="T3">Type of argument 3.</typeparam>
        /// <typeparam name="T4">Type of argument 4.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <param name="arg3">Argument 3.</param>
        /// <param name="arg4">Argument 4.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action,
                                                   T1 arg1,
                                                   T2 arg2,
                                                   T3 arg3,
                                                   T4 arg4,
                                                   LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <typeparam name="T2">Type of argument 2.</typeparam>
        /// <typeparam name="T3">Type of argument 3.</typeparam>
        /// <typeparam name="T4">Type of argument 4.</typeparam>
        /// <typeparam name="T5">Type of argument 5.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <param name="arg3">Argument 3.</param>
        /// <param name="arg4">Argument 4.</param>
        /// <param name="arg5">Argument 5.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action,
                                                       T1 arg1,
                                                       T2 arg2,
                                                       T3 arg3,
                                                       T4 arg4,
                                                       T5 arg5,
                                                       LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1, arg2, arg3, arg4, arg5);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <typeparam name="T2">Type of argument 2.</typeparam>
        /// <typeparam name="T3">Type of argument 3.</typeparam>
        /// <typeparam name="T4">Type of argument 4.</typeparam>
        /// <typeparam name="T5">Type of argument 5.</typeparam>
        /// <typeparam name="T6">Type of argument 6.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <param name="arg3">Argument 3.</param>
        /// <param name="arg4">Argument 4.</param>
        /// <param name="arg5">Argument 5.</param>
        /// <param name="arg6">Argument 6.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action,
                                                           T1 arg1,
                                                           T2 arg2,
                                                           T3 arg3,
                                                           T4 arg4,
                                                           T5 arg5,
                                                           T6 arg6,
                                                           LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1, arg2, arg3, arg4, arg5, arg6);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <typeparam name="T2">Type of argument 2.</typeparam>
        /// <typeparam name="T3">Type of argument 3.</typeparam>
        /// <typeparam name="T4">Type of argument 4.</typeparam>
        /// <typeparam name="T5">Type of argument 5.</typeparam>
        /// <typeparam name="T6">Type of argument 6.</typeparam>
        /// <typeparam name="T7">Type of argument 7.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <param name="arg3">Argument 3.</param>
        /// <param name="arg4">Argument 4.</param>
        /// <param name="arg5">Argument 5.</param>
        /// <param name="arg6">Argument 6.</param>
        /// <param name="arg7">Argument 7.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action,
                                                               T1 arg1,
                                                               T2 arg2,
                                                               T3 arg3,
                                                               T4 arg4,
                                                               T5 arg5,
                                                               T6 arg6,
                                                               T7 arg7,
                                                               LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }

        /// <summary>
        /// Invokes the given <see cref="Action" />. If the action throws an exception,
        /// logs it at the given <see cref="LogEventLevel" /> but doesn't allow it to bubble up to the caller.
        /// </summary>
        /// <typeparam name="T1">Type of argument 1.</typeparam>
        /// <typeparam name="T2">Type of argument 2.</typeparam>
        /// <typeparam name="T3">Type of argument 3.</typeparam>
        /// <typeparam name="T4">Type of argument 4.</typeparam>
        /// <typeparam name="T5">Type of argument 5.</typeparam>
        /// <typeparam name="T6">Type of argument 6.</typeparam>
        /// <typeparam name="T7">Type of argument 7.</typeparam>
        /// <typeparam name="T8">Type of argument 8.</typeparam>
        /// <param name="action">Action to invoke.</param>
        /// <param name="arg1">Argument 1.</param>
        /// <param name="arg2">Argument 2.</param>
        /// <param name="arg3">Argument 3.</param>
        /// <param name="arg4">Argument 4.</param>
        /// <param name="arg5">Argument 5.</param>
        /// <param name="arg6">Argument 6.</param>
        /// <param name="arg7">Argument 7.</param>
        /// <param name="arg8">Argument 8.</param>
        /// <param name="logLevel">(optional, default = Error) Log level at which the exception will be logged.</param>
        public static void Swallow<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action,
                                                                   T1 arg1,
                                                                   T2 arg2,
                                                                   T3 arg3,
                                                                   T4 arg4,
                                                                   T5 arg5,
                                                                   T6 arg6,
                                                                   T7 arg7,
                                                                   T8 arg8,
                                                                   LogEventLevel logLevel = LogEventLevel.Error)
        {
            try
            {
                action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Write(logLevel, ex, "Action failed");
            }
        }
    }
}
