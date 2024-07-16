using System;
using System.Diagnostics.CodeAnalysis;

namespace Soundmouse.Utils.Extensions
{
    /// <summary>
    /// Class containing extensions for <see cref="DateTime"/>.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// UNIX's epoch.
        /// </summary>
        public static DateTime Epoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        /// <summary>
        /// Gets the current UTC moment as milliseconds since UNIX epoch.
        /// </summary>
        /// <value>Milliseconds since epoch to the current moment in UTC time.</value>
        [ExcludeFromCodeCoverage]
        public static long UnixUtcNow => DateTime.UtcNow.ToUnixMilliseconds();
        
        /// <summary>
        /// Converts the given input to its representation as Microseconds since UNIX epoch.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the amount of Microseconds passed between UNIX epoch and the input.</returns>
        /// <exception cref="System.ArgumentException">Input must occur after UNIX epoch - input</exception>
        /// <value>Microseconds since epoch to the given input.</value>
        public static long ToUnixMicroseconds(this DateTime input)
        {
            if (input < Epoch)
                throw new ArgumentException("Input must occur after UNIX epoch", nameof(input));

            return (input - Epoch).Ticks / 10;
        }

        /// <summary>
        /// Converts the given input to its representation as milliseconds since UNIX epoch.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the amount of milliseconds passed between UNIX epoch and the input.</returns>
        /// <exception cref="System.ArgumentException">Input must occur after UNIX epoch - input</exception>
        /// <value>Milliseconds since epoch to the given input.</value>
        public static long ToUnixMilliseconds(this DateTime input)
        {
            if (input < Epoch)
                throw new ArgumentException("Input must occur after UNIX epoch", nameof(input));

            return (long) Math.Round((input - Epoch).TotalMilliseconds);
        }

        /// <summary>
        /// Converts the given input to its representation as seconds since UNIX epoch.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>Returns the amount of seconds passed between UNIX epoch and the input.</returns>
        /// <exception cref="System.ArgumentException">Input must occur after UNIX epoch - input</exception>
        /// <value>Seconds since epoch to the given input.</value>
        public static long ToUnixSeconds(this DateTime input)
        {
            if (input < Epoch)
                throw new ArgumentException("Input must occur after UNIX epoch", nameof(input));

            return (long) Math.Round((input - Epoch).TotalSeconds);
        }

        /// <summary>
        /// Creates a new <see cref="DateTime"/> by adding the given amount of ticks to the UNIX epoch.
        /// </summary>
        /// <param name="ticksSinceEpoch">Input to add.</param>
        /// <returns>Returns a <see cref="DateTime"/> that represents the given amount of ticks added to the UNIX epoch.</returns>
        public static DateTime FromUnixTime(long ticksSinceEpoch) => Epoch.AddTicks(ticksSinceEpoch);

        /// <summary>
        /// Changes the year in the given input.
        /// </summary>
        /// <param name="input">Input to change.</param>
        /// <param name="year">New input's year.</param>
        /// <returns>Returns the input with the given year.</returns>
        /// <exception cref="System.ArgumentException">Year must be positive - year</exception>
        public static DateTime WithYear(this DateTime input, int year)
        {
            if (year < 1)
                throw new ArgumentException("Year must be positive", nameof(year));

            // Account for leap years
            var day = input.Day;
            if (DateTime.DaysInMonth(year, input.Month) < day) 
                day = DateTime.DaysInMonth(year, input.Month);

            return new DateTime(year, 
                                input.Month, 
                                day, 
                                input.Hour, 
                                input.Minute, 
                                input.Second, 
                                input.Millisecond, 
                                input.Kind);
        }

        /// <summary>
        /// Changes the month in the given input.
        /// </summary>
        /// <param name="input">Input to change.</param>
        /// <param name="month">New input's month.</param>
        /// <returns>Returns the input with the given year.</returns>
        /// <exception cref="System.ArgumentException">Year must be positive - year</exception>
        public static DateTime WithMonth(this DateTime input, int month)
        {
            if (month < 1 || month > 12)
                throw new ArgumentException("Month must be positive", nameof(month));

            // Account for leap years
            var day = input.Day;
            if (DateTime.DaysInMonth(input.Year, month) < day) 
                day = DateTime.DaysInMonth(input.Year, month);

            return new DateTime(input.Year, 
                                month, 
                                day, 
                                input.Hour, 
                                input.Minute, 
                                input.Second, 
                                input.Millisecond, 
                                input.Kind);
        }

        /// <summary>
        /// Changes the day in the given input.
        /// </summary>
        /// <param name="input">Input to change.</param>
        /// <param name="day">New input's day.</param>
        /// <returns>Returns the input with the given day.</returns>
        /// <exception cref="ArgumentException">Day must be positive - day</exception>
        public static DateTime WithDay(this DateTime input,
                                       int day)
        {
            if (day < 1 || day > 31)
                throw new ArgumentException("Day must be positive", nameof(day));

            if (DateTime.DaysInMonth(input.Year, input.Month) < day)
                day = DateTime.DaysInMonth(input.Year, input.Month);

            return new DateTime(input.Year,
                                input.Month,
                                day,
                                input.Hour,
                                input.Minute,
                                input.Second,
                                input.Millisecond,
                                input.Kind);
        }

        /// <summary>
        /// Gets the number of the day of the week of the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="input">Input.</param>
        /// <returns>Returns the number of the day of the week.</returns>
        public static int DayOfWeekNumber(this DateTime input) => (int) (input.DayOfWeek + 6) % 7;

        /// <summary>
        /// Remove the milliseconds component of the input.
        /// </summary>
        /// <param name="input">Input to remove the milliseconds component.</param>
        /// <returns>Returns the input without the milliseconds component.</returns>
        public static DateTime WithoutMilliseconds(this DateTime input)
        {
            return new DateTime(input.Year, input.Month, input.Day, input.Hour, input.Minute, input.Second);
        }

        /// <summary>
        /// Converts local date/time to UTC using timezone informaiton.
        /// </summary>
        /// <param name="input">Local date and time</param>
        /// <param name="timezoneId">Timezone ID of the local time</param>
        /// <returns>Returns the input as UTC.</returns>
        public static DateTime ConvertToUtc(this DateTime input, string timezoneId)
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            
            return TimeZoneInfo.ConvertTimeToUtc(new DateTime(input.Ticks, DateTimeKind.Unspecified), timezone);
        }

        /// <summary>
        /// Converts the input to its UTC representation.
        /// </summary>
        /// <param name="input">Input to convert.</param>
        /// <returns>If the input is valid (not null), returns its UTC representation; Otherwise, returns null.</returns>
        public static DateTime? ToNullableUniversalTime(this DateTime? input) => input?.ToUniversalTime();
    }
}