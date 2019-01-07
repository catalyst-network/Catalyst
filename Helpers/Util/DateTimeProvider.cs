using System;

namespace ADL.Util
{
    public static class DateTimeProvider
    {
        
        private static readonly Func<DateTime> Current = () => DateTime.UtcNow;

        public static DateTime UtcNow => Current();
    }
}
