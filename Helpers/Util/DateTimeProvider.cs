using System;

namespace ADL.Util
{
    public static class DateTimeProvider
    {
        public static DateTime UtcNow => Current();
        private static readonly Func<DateTime> Current = () => DateTime.UtcNow;
    }
}
