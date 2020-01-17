#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Catalyst.Core.Lib.Util
{
    /// <summary>
    ///   Parsing and stringifying of a <see cref="TimeSpan"/> according to IPFS.
    /// </summary>
    /// <remarks>
    ///   From the <see href="https://godoc.org/time#ParseDuration">go spec</see>.
    ///   <para>
    ///   A duration string is a possibly signed sequence of decimal numbers, 
    ///   each with optional fraction and a unit suffix, such as "300ms", "-1.5h" or "2h45m". 
    ///   Valid time units are "ns", "us" (or "µs"), "ms", "s", "m", "h".
    ///   </para>
    /// </remarks>
    public static class Duration
    {
        private const double TicksPerNanosecond = TimeSpan.TicksPerMillisecond * 0.000001;
        private const double TicksPerMicrosecond = TimeSpan.TicksPerMillisecond * 0.001;

        /// <summary>
        ///   Converts the string representation of an IPFS duration
        ///   into its <see cref="TimeSpan"/> equivalent.
        /// </summary>
        /// <param name="s">
        ///   A string that contains the duration to convert.
        /// </param>
        /// <returns>
        ///   A <see cref="TimeSpan"/> that is equivalent to <paramref name="s"/>.
        /// </returns>
        /// <exception cref="FormatException">
        ///   <paramref name="s"/> is not a valid IPFS duration.
        /// </exception>
        /// <remarks>
        ///   An empty string or "n/a" or "unknown" returns <see cref="TimeSpan.Zero"/>.
        ///   <para>
        ///   A duration string is a possibly signed sequence of decimal numbers, 
        ///   each with optional fraction and a unit suffix, such as "300ms", "-1.5h" or "2h45m". 
        ///   Valid time units are "ns", "us" (or "µs"), "ms", "s", "m", "h".
        ///   </para>
        /// </remarks>
        public static TimeSpan Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "n/a" || s == "unknown")
                return TimeSpan.Zero;

            var result = TimeSpan.Zero;
            var negative = false;
            using (var sr = new StringReader(s))
            {
                if (sr.Peek() == '-')
                {
                    negative = true;
                    sr.Read();
                }

                while (sr.Peek() != -1)
                {
                    result += ParseComponent(sr);
                }
            }

            if (negative)
                return TimeSpan.FromTicks(-result.Ticks);

            return result;
        }

        private static TimeSpan ParseComponent(StringReader reader)
        {
            var value = ParseNumber(reader);
            var unit = ParseUnit(reader);

            switch (unit)
            {
                case "h":
                    return TimeSpan.FromHours(value);
                case "m":
                    return TimeSpan.FromMinutes(value);
                case "s":
                    return TimeSpan.FromSeconds(value);
                case "ms":
                    return TimeSpan.FromMilliseconds(value);
                case "us":
                case "µs":
                    return TimeSpan.FromTicks((long) (value * TicksPerMicrosecond));
                case "ns":
                    return TimeSpan.FromTicks((long) (value * TicksPerNanosecond));
                case "":
                    throw new FormatException("Missing IPFS duration unit.");
                default:
                    throw new FormatException($"Unknown IPFS duration unit '{unit}'.");
            }
        }

        private static double ParseNumber(StringReader reader)
        {
            var s = new StringBuilder();
            while (true)
            {
                var c = (char) reader.Peek();
                if (char.IsDigit(c) || c == '.')
                {
                    s.Append(c);
                    reader.Read();
                    continue;
                }

                return double.Parse(s.ToString(), CultureInfo.InvariantCulture);
            }
        }

        private static string ParseUnit(StringReader reader)
        {
            var s = new StringBuilder();
            while (true)
            {
                var c = (char) reader.Peek();
                if (char.IsDigit(c) || c == '.' || c == (char) 0xFFFF)
                    break;
                s.Append(c);
                reader.Read();
            }

            return s.ToString();
        }

        /// <summary>
        ///   Converts the <see cref="TimeSpan"/> to the equivalent string representation of an 
        ///   IPFS duration.
        /// </summary>
        /// <param name="duration">
        ///   The <see cref="TimeSpan"/> to convert.
        /// </param>
        /// <param name="zeroValue">
        ///   The string representation, when the <paramref name="duration"/> 
        ///   is equal to <see cref="TimeSpan.Zero"/>/
        /// </param>
        /// <returns>
        ///   The IPFS duration string representation.
        /// </returns>
        public static string Stringify(TimeSpan duration, string zeroValue = "0s")
        {
            if (duration.Ticks == 0)
                return zeroValue;

            var s = new StringBuilder();
            if (duration.Ticks < 0)
            {
                s.Append('-');
                duration = TimeSpan.FromTicks(-duration.Ticks);
            }

            Stringify(Math.Floor(duration.TotalHours), "h", s);
            Stringify(duration.Minutes, "m", s);
            Stringify(duration.Seconds, "s", s);
            Stringify(duration.Milliseconds, "ms", s);
            Stringify((long) (duration.Ticks / TicksPerMicrosecond) % 1000, "us", s);
            Stringify((long) (duration.Ticks / TicksPerNanosecond) % 1000, "ns", s);

            return s.ToString();
        }

        private static void Stringify(double value, string unit, StringBuilder sb)
        {
            if (value == 0)
            {
                return;
            }
            
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            sb.Append(unit);
        }
    }
}
