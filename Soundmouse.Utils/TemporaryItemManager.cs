using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;

using Serilog;

using Soundmouse.Utils.Extensions;
using Soundmouse.Utils.Utilities;

namespace Soundmouse.Utils
{
    /// <summary>
    /// Defines the methods expose by a manager capable of creating temporary items.
    /// </summary>
    public interface ITemporaryItemManager
    {
        /// <summary>
        /// Creates a new temporary file and returns it's path.
        /// </summary>
        /// <param name="extension">(Optional) Extension of the temporary file.</param>
        /// <returns>Path to the new temporary file.</returns>
        string CreateTemporaryFile(string? extension = null);

        /// <summary>
        /// Creates a new temporary directory and returns it's path.
        /// </summary>
        /// <returns>Path to the new temporary directory.</returns>
        string CreateTemporaryDirectory();
    }

    /// <summary>
    /// Temporary item manager that creates and keeps track of all the created temporary items (directories and files).
    /// </summary>
    public sealed class TemporaryItemManager : ITemporaryItemManager
    {
        #region Private fields

        private readonly ConcurrentBag<string> _generatedTempFiles = new ConcurrentBag<string>();
        private readonly ConcurrentBag<string> _generatedTempDirectories = new ConcurrentBag<string>();
        private readonly object _directoryCreationObj = new object();

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the temporary directory path.
        /// </summary>
        /// <value>The temporary directory path.</value>
        public string TemporaryDirectoryPath { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TemporaryItemManager"/> class.
        /// </summary>
        /// <param name="temporaryDirectoryPath">Path to the temp directory.</param>
        public TemporaryItemManager(string? temporaryDirectoryPath = null)
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            TemporaryDirectoryPath = string.IsNullOrWhiteSpace(temporaryDirectoryPath) 
                                          ? Path.GetTempPath() 
                                          : temporaryDirectoryPath;
#pragma warning restore CS8601 // Possible null reference assignment.
        }

        #endregion

        #region Public methods

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string CreateTemporaryFile(string? extension = null)
        {
            var directoryPath = Path.Combine(TemporaryDirectoryPath,
                                             DateTime.UtcNow.Date.ToString("yyyyMMdd"));

            if (!Directory.Exists(directoryPath))
            {
                lock (_directoryCreationObj)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
            }

            string tempFilePath = Path.Combine(directoryPath, Guid.NewGuid() + ".tmp");
            if (!string.IsNullOrWhiteSpace(extension))
                tempFilePath = Path.ChangeExtension(tempFilePath, extension);

            using (File.Create(tempFilePath))
            {
            }

            _generatedTempFiles.Add(tempFilePath);

            if (Log.Logger.IsInformationEnabled())
                Log.Information("Created new temporary file at '{FilePath}' for '{Caller}'.",
                                tempFilePath,
                                MiscUtilities.GetCallingType(1 /*skip this frame*/));

            return tempFilePath;
        }
        
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string CreateTemporaryDirectory()
        {
            var directoryPath = Path.Combine(TemporaryDirectoryPath,
                                             DateTime.UtcNow.Date.ToString("yyyyMMdd"));

            if (!Directory.Exists(directoryPath))
            {
                lock (_directoryCreationObj)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
            }

            string tempDirectoryPath = Path.Combine(directoryPath, Guid.NewGuid().ToString());

            Directory.CreateDirectory(tempDirectoryPath);

            _generatedTempDirectories.Add(tempDirectoryPath);

            if (Log.Logger.IsInformationEnabled())
                Log.Information("Created new temporary directory at '{DirectoryPath}' for '{Caller}'.",
                                tempDirectoryPath,
                                MiscUtilities.GetCallingType(1 /*skip this frame*/));

            return tempDirectoryPath;
        }

        /// <summary>
        /// Clears the temporary items that were generated during this AppDomain's lifetime (unless cleared before hand).
        /// </summary>
        public void ClearGeneratedItems()
        {
            while(_generatedTempDirectories.TryTake(out string directory))
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            
            while(_generatedTempFiles.TryTake(out string file))
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }

        #endregion
    }
}
