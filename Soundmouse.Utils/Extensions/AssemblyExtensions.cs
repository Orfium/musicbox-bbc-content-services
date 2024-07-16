using System;
using System.IO;
using System.Reflection;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Class containing extensions for <see cref="Assembly"/>.
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Gets the directory where the <see cref="Assembly"/> is located.
        /// </summary>
        /// <param name="assembly">Assembly.</param>
        /// <returns>Returns the <see cref="Assembly"/>'s directory.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to find assembly directory</exception>
        public static string GetDirectory(this Assembly assembly)
        {
            if(assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            
            var directoryName = Path.GetDirectoryName(assembly.Location);
            if(string.IsNullOrWhiteSpace(directoryName))
                throw new InvalidOperationException($"Unable to find assembly {assembly.FullName} directory");

            return directoryName;
        }
    }
}
