using System;

namespace ADL.Util
{
    static class TimeExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        internal static TimeSpan Minutes(this int seconds)
        {
            if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            return TimeSpan.FromMinutes(seconds);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        internal static TimeSpan Seconds(this int seconds)
        {
            if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            return TimeSpan.FromSeconds(seconds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        internal static TimeSpan Milliseconds(this int milliseconds)
        {
            if (milliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(milliseconds));
            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }
}
