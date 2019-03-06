using System;
using Dawn;

namespace Catalyst.Node.Common.Helpers.Util
{
    public static class TimeExtensions
    {
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
    }
}