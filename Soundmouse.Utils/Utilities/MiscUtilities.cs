using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Soundmouse.Utils.Utilities
{
    /// <summary>
    /// Contains various helpers and extensions that do not fit a specific type / domain.
    /// </summary>
    public static class MiscUtilities
    {
        /// <summary>
        /// Searches for a given environment variable across the process, user and machine environment variable sets.
        /// </summary>
        /// <param name="environmentVariableName">Name of the environment variable to retrieve.</param>
        /// <returns>If <paramref name="environmentVariableName"/> exists in the environment variables, returns its value; Otherwise, returns an empty string.</returns>
        public static string GetEnvironmentVariable(string environmentVariableName)
        {
            // Allow destinations to override the FFMPEG root path (in case they want to use different binaries)
            string environmentVariable = Environment.GetEnvironmentVariable(environmentVariableName,EnvironmentVariableTarget.Process);
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentVariableName,EnvironmentVariableTarget.User)))
                environmentVariable = Environment.GetEnvironmentVariable(environmentVariableName,EnvironmentVariableTarget.User);
            else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentVariableName,EnvironmentVariableTarget.Machine)))
                environmentVariable = Environment.GetEnvironmentVariable(environmentVariableName,EnvironmentVariableTarget.Machine);

            return environmentVariable ?? string.Empty;
        }

        /// <summary>
        /// Determines whether the current environment is UNIX based.
        /// </summary>
        /// <returns>Returns <c>true</c> if current environment is UNIX based; Otherwise, returns <c>false</c>.</returns>
        public static bool IsUnix()
        {
            return Environment.OSVersion.Platform switch
                   {
                       PlatformID.MacOSX => true,
                       PlatformID.Unix   => true,
                       _                 => false
                   };
        }

        /// <summary>
        /// Gets the name of of the calling type.
        /// </summary>
        /// <param name="framesToSkip">Number of frames to skip to get the calling type.</param>
        /// <returns>Returns the name of the calling type after skipping N frames.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetCallingType(int framesToSkip)
        {
            string callingType;
            Type declaringType;
            framesToSkip += 1; // Skipping this frame
            do
            {
                MethodBase method = new StackFrame(framesToSkip, false).GetMethod();

                declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    callingType = method.Name;

                    break;
                }

                framesToSkip++;
                callingType = $"{declaringType.FullName}::{method.Name}";
            } while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

            return callingType;
        }

        /// <summary>
        /// Calculates the MD5 hash of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="input">Stream to hash.</param>
        /// <returns>Returns the MD5 hash of the <see cref="Stream"/>.</returns>
        public static string CalculateMd5(this Stream input)
        {
            if (input.Position > 0)
                input.Seek(0, SeekOrigin.Begin);
            
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(input);
            var sb = new StringBuilder(hash.Length * 2);
            
            foreach (var @byte in hash)
            {
                sb.AppendFormat("{0:x2}", @byte);
            }

            return sb.ToString().ToLower();
        }

        /// <summary>
        /// Determines whether a given IP address is a LAN IP.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns></returns>
        /// <remarks>
        /// Based on private addresses range: rfc1918
        /// 
        /// 10.0.0.0        -   10.255.255.255  (10/8 prefix)
        /// 172.16.0.0      -   172.31.255.255  (172.16/12 prefix)
        /// 192.168.0.0     -   192.168.255.255 (192.168/16 prefix)
        /// 
        /// </remarks>
        public static bool IsLanIp(IPAddress address)
        {
            if (address.Equals(IPAddress.IPv6Loopback) || address.Equals(IPAddress.Loopback))
                return true;

            var addressPartsStr = address.ToString()
                                         .Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries)
                                         .ToArray();

            if (addressPartsStr.Length != 4)
                return false;

            var segmentZero = int.Parse(addressPartsStr[0]);
            
            // IP Address could "probably" be public. This doesn't catch some VPN ranges like OpenVPN and Hamachi.

            if (segmentZero == 10) 
                return true;

            var segmentOne = int.Parse(addressPartsStr[1]);

            return segmentZero == 192 && segmentOne == 168 
                   || segmentZero == 172 && segmentOne >= 16 && segmentOne <= 31;
        }
        
        private static readonly Random _random = new Random();

        /// <summary>
        /// Generates a random alphanumeric string with the desired length.
        /// </summary>
        /// <param name="length">Length of the random string.</param>
        /// <returns>Returns the generated random string.</returns>
        public static string GenerateRandomString(ushort length)
        {
            const string chars = "abcfefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Coalesces the given arguments, returning the first argument that is not the default value of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args">Arguments to evaluate.</param>
        /// <returns>Returns the first argument that is not the default value of <typeparamref name="T"/>.
        /// If all arguments equal the default value of <typeparamref name="T"/>, returns the default of <typeparamref name="T"/>.
        /// </returns>
        public static T Coalesce<T>(params T[] args)
        {
            var comparer = EqualityComparer<T>.Default;
            var defaultT = default(T);

            foreach (var arg in args)
            {
                if (!comparer.Equals(arg, defaultT))
                {
                    return arg;
                }
            }

            return defaultT;
        }
    }
}
