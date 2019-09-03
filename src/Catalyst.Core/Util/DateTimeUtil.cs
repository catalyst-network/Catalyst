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
using Dawn;

namespace Catalyst.Core.Util
{
    public static class DateTimeUtil
    {
        private static readonly Func<DateTime> Current = () => DateTime.UtcNow;
        public static DateTime UtcNow => Current();

        /// <summary>
        /// </summary>
        /// <param name="mins"></param>
        /// <returns></returns>
        public static TimeSpan Minutes(this int mins)
        {
            Guard.Argument(mins, nameof(mins)).Min(1);
            return TimeSpan.FromMinutes(mins);
        }

        /// <summary>
        /// </summary>
        /// <param name="secs"></param>
        /// <returns></returns>
        public static TimeSpan Seconds(this int secs)
        {
            Guard.Argument(secs, nameof(secs)).Min(1);
            return TimeSpan.FromSeconds(secs);
        }

        /// <summary>
        /// </summary>
        /// <param name="milliSecs"></param>
        /// <returns></returns>
        public static TimeSpan Milliseconds(this int milliSecs)
        {
            Guard.Argument(milliSecs, nameof(milliSecs)).Min(1);
            return TimeSpan.FromMilliseconds(milliSecs);
        }

        public static TimeSpan GetExponentialTimeSpan(int seed)
        {
            return TimeSpan.FromMilliseconds(Math.Pow(2, seed));
        }
    }
}
