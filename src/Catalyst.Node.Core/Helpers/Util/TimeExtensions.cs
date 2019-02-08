using System;
using Dawn;

namespace Catalyst.Node.Core.Helpers.Util
{
    public static class TimeExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public static TimeSpan Minutes(this int minutes)
        {
            Guard.Argument(minutes, nameof(minutes)).Min(1);
            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static TimeSpan Seconds(this int seconds)
        {
            Guard.Argument(seconds, nameof(seconds)).Min(1);
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static TimeSpan Milliseconds(this int milliseconds)
        {
            Guard.Argument(milliseconds, nameof(milliseconds)).Min(1);
            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }
}