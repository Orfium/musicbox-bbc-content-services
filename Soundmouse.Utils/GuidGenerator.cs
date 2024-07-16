﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
#pragma warning disable 1591

namespace Soundmouse.Utils
{
    internal enum GuidGeneration
    {
        Fast,
        NoDuplicates
    }

    public enum GuidVersion
    {
        TimeBased = 0x01,
        Random = 0x04
    }

    /// <summary>
    /// Used for generating UUID based on RFC 4122.
    /// </summary>
    /// <seealso href="http://www.ietf.org/rfc/rfc4122.txt">RFC 4122 - A Universally Unique IDentifier (UUID) URN Namespace</seealso>
    [ExcludeFromCodeCoverage]
    public static class GuidGenerator
    {
        private static readonly Random _random;
        private static readonly object _lock = new object();

        private static DateTimeOffset _lastTimestampForNoDuplicatesGeneration = TimestampHelper.UtcNow();

        // number of bytes in uuid
        private const int ByteArraySize = 16;

        // multiplex variant info
        private const int VariantByte = 8;
        private const int VariantByteMask = 0x3f;
        private const int VariantByteShift = 0x80;

        // multiplex version info
        private const int VersionByte = 7;
        private const int VersionByteMask = 0x0f;
        private const int VersionByteShift = 4;

        // indexes within the uuid array for certain boundaries
        private const byte TimestampByte = 0;
        private const byte GuidClockSequenceByte = 8;
        private const byte NodeByte = 10;

        // offset to move from 1/1/0001, which is 0-time for .NET, to gregorian 0-time of 10/15/1582
        private static readonly DateTimeOffset _gregorianCalendarStart = new DateTimeOffset(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

        internal static GuidGeneration GuidGeneration { get; set; }

        public static byte[] NodeBytes { get; set; }
        public static byte[] ClockSequenceBytes { get; set; }

        static GuidGenerator()
        {
            _random = new Random();

            GuidGeneration = GuidGeneration.NoDuplicates;
            NodeBytes = GenerateNodeBytes();
            ClockSequenceBytes = GenerateClockSequenceBytes();
        }

        /// <summary>
        /// Generates a random value for the node.
        /// </summary>
        /// <returns></returns>
        public static byte[] GenerateNodeBytes()
        {
            var node = new byte[6];

            _random.NextBytes(node);
            return node;
        }

        /// <summary>
        /// Generates a node based on the first 6 bytes of an IP address.
        /// </summary>
        /// <param name="ip"></param>
        public static byte[] GenerateNodeBytes(IPAddress ip)
        {
            if (ip == null)
                throw new ArgumentNullException("ip");

            var bytes = ip.GetAddressBytes();

            if (bytes.Length < 6)
                throw new ArgumentOutOfRangeException("ip", "The passed in IP address must contain at least 6 bytes.");

            var node = new byte[6];
            Array.Copy(bytes, node, 6);

            return node;
        }

        /// <summary>
        /// Generates a node based on the bytes of the MAC address.
        /// </summary>
        /// <param name="mac"></param>
        /// <remarks>The machines MAC address can be retrieved from <see cref="NetworkInterface.GetPhysicalAddress"/>.</remarks>
        public static byte[] GenerateNodeBytes(PhysicalAddress mac)
        {
            if (mac == null)
                throw new ArgumentNullException("mac");

            var node = mac.GetAddressBytes();

            return node;
        }

        /// <summary>
        /// Generates a random clock sequence.
        /// </summary>
        public static byte[] GenerateClockSequenceBytes()
        {
            var bytes = new byte[2];
            _random.NextBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// In order to maintain a constant value we need to get a two byte hash from the DateTime.
        /// </summary>
        public static byte[] GenerateClockSequenceBytes(DateTime dt)
        {
            var utc = dt.ToUniversalTime();
            return GenerateClockSequenceBytes(utc.Ticks);
        }

        /// <summary>
        /// In order to maintain a constant value we need to get a two byte hash from the DateTime.
        /// </summary>
        public static byte[] GenerateClockSequenceBytes(DateTimeOffset dt)
        {
            var utc = dt.ToUniversalTime();
            return GenerateClockSequenceBytes(utc.Ticks);
        }

        public static byte[] GenerateClockSequenceBytes(long ticks)
        {
            var bytes = BitConverter.GetBytes(ticks);

            if (bytes.Length == 0)
                return new byte[] {0x0, 0x0};

            if (bytes.Length == 1)
                return new byte[] {0x0, bytes[0]};

            return new[] {bytes[0], bytes[1]};
        }

        public static GuidVersion GetUuidVersion(this Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            return (GuidVersion) ((bytes[VersionByte] & 0xFF) >> VersionByteShift);
        }

        public static DateTimeOffset GetDateTimeOffset(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();

            // reverse the version
            bytes[VersionByte] &= VersionByteMask;
            bytes[VersionByte] |= (byte) GuidVersion.TimeBased >> VersionByteShift;

            byte[] timestampBytes = new byte[8];
            Array.Copy(bytes, TimestampByte, timestampBytes, 0, 8);

            long timestamp = BitConverter.ToInt64(timestampBytes, 0);
            long ticks = timestamp + _gregorianCalendarStart.Ticks;

            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        public static DateTime GetDateTime(Guid guid)
        {
            return GetDateTimeOffset(guid).DateTime;
        }

        public static DateTime GetLocalDateTime(Guid guid)
        {
            return GetDateTimeOffset(guid).LocalDateTime;
        }

        public static DateTime GetUtcDateTime(Guid guid)
        {
            return GetDateTimeOffset(guid).UtcDateTime;
        }

        public static Guid GenerateTimeBasedGuid()
        {
            switch (GuidGeneration)
            {
                case GuidGeneration.Fast:
                    return GenerateTimeBasedGuid(TimestampHelper.UtcNow(), ClockSequenceBytes, NodeBytes);

                case GuidGeneration.NoDuplicates:
                default:
                    lock (_lock)
                    {
                        var ts = TimestampHelper.UtcNow();

                        if (ts <= _lastTimestampForNoDuplicatesGeneration)
                            ClockSequenceBytes = GenerateClockSequenceBytes();

                        _lastTimestampForNoDuplicatesGeneration = ts;

                        return GenerateTimeBasedGuid(ts, ClockSequenceBytes, NodeBytes);
                    }
            }
        }

        public static Guid GenerateTimeBasedGuid(DateTime dateTime)
        {
            return GenerateTimeBasedGuid(dateTime, GenerateClockSequenceBytes(dateTime), NodeBytes);
        }

        public static Guid GenerateTimeBasedGuid(DateTimeOffset dateTime)
        {
            return GenerateTimeBasedGuid(dateTime, GenerateClockSequenceBytes(dateTime), NodeBytes);
        }

        public static Guid GenerateTimeBasedGuid(DateTime dateTime, PhysicalAddress mac)
        {
            return GenerateTimeBasedGuid(dateTime, GenerateClockSequenceBytes(dateTime), GenerateNodeBytes(mac));
        }

        public static Guid GenerateTimeBasedGuid(DateTimeOffset dateTime, PhysicalAddress mac)
        {
            return GenerateTimeBasedGuid(dateTime, GenerateClockSequenceBytes(dateTime), GenerateNodeBytes(mac));
        }

        public static Guid GenerateTimeBasedGuid(DateTime dateTime, IPAddress ip)
        {
            return GenerateTimeBasedGuid(dateTime, GenerateClockSequenceBytes(dateTime), GenerateNodeBytes(ip));
        }

        public static Guid GenerateTimeBasedGuid(DateTimeOffset dateTime, IPAddress ip)
        {
            return GenerateTimeBasedGuid(dateTime, GenerateClockSequenceBytes(dateTime), GenerateNodeBytes(ip));
        }

        public static Guid GenerateTimeBasedGuid(DateTime dateTime, byte[] clockSequence, byte[] node)
        {
            return GenerateTimeBasedGuid(new DateTimeOffset(dateTime), clockSequence, node);
        }

        public static Guid GenerateTimeBasedGuid(DateTimeOffset dateTime, byte[] clockSequence, byte[] node)
        {
            if (clockSequence == null)
                throw new ArgumentNullException("clockSequence");

            if (node == null)
                throw new ArgumentNullException("node");

            if (clockSequence.Length != 2)
                throw new ArgumentOutOfRangeException("clockSequence", "The clockSequence must be 2 bytes.");

            if (node.Length != 6)
                throw new ArgumentOutOfRangeException("node", "The node must be 6 bytes.");

            long ticks = (dateTime - _gregorianCalendarStart).Ticks;
            byte[] guid = new byte[ByteArraySize];
            byte[] timestamp = BitConverter.GetBytes(ticks);

            // copy node
            Array.Copy(node, 0, guid, NodeByte, Math.Min(6, node.Length));

            // copy clock sequence
            Array.Copy(clockSequence, 0, guid, GuidClockSequenceByte, Math.Min(2, clockSequence.Length));

            // copy timestamp
            Array.Copy(timestamp, 0, guid, TimestampByte, Math.Min(8, timestamp.Length));

            // set the variant
            guid[VariantByte] &= VariantByteMask;
            guid[VariantByte] |= VariantByteShift;

            // set the version
            guid[VersionByte] &= VersionByteMask;
            guid[VersionByte] |= (byte) GuidVersion.TimeBased << VersionByteShift;

            return new Guid(guid);
        }

        private static class TimestampHelper
        {
            /// <summary>
            /// Allows for the use of alternative timestamp providers.
            /// </summary>
            public static readonly Func<DateTimeOffset> UtcNow = () => DateTimePrecise.UtcNowOffset;

            private class DateTimePrecise
            {
                private static readonly DateTimePrecise _instance = new DateTimePrecise();

                public static DateTimeOffset UtcNowOffset
                {
                    get { return _instance.GetUtcNow(); }
                }

                private readonly double _divergentSeconds;
                private readonly double _syncSeconds;
                private readonly Stopwatch _stopwatch;
                private DateTimeOffset _baseTime;

                private DateTimePrecise(int syncSeconds = 1, int divergentSeconds = 1)
                {
                    _syncSeconds = syncSeconds;
                    _divergentSeconds = divergentSeconds;

                    _stopwatch = new Stopwatch();

                    Syncronize();
                }

                private void Syncronize()
                {
                    lock (_stopwatch)
                    {
                        _baseTime = DateTimeOffset.UtcNow;
                        _stopwatch.Restart();
                    }
                }

                private DateTimeOffset GetUtcNow()
                {
                    var now = DateTimeOffset.UtcNow;
                    var elapsed = _stopwatch.Elapsed;

                    if (elapsed.TotalSeconds > _syncSeconds)
                    {
                        Syncronize();

                        // account for any time that has passed since the stopwatch was syncronized
                        elapsed = _stopwatch.Elapsed;
                    }

                    /**
                     * The Stopwatch has many bugs associated with it, so when we are in doubt of the results
                     * we are going to default to DateTimeOffset.UtcNow
                     * http://stackoverflow.com/questions/1008345
                     **/

                    // check for elapsed being less than zero
                    if (elapsed < TimeSpan.Zero)
                        return now;

                    var preciseNow = _baseTime + elapsed;

                    // make sure the two clocks don't diverge by more than defined seconds
                    if (Math.Abs((preciseNow - now).TotalSeconds) > _divergentSeconds)
                        return now;

                    return _baseTime + elapsed;
                }
            }
        }
    }
}