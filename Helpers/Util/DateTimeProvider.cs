using System;

namespace ADL.Util
{
    public static class DateTimeProvider
    {
        public static Func<DateTime> Current = () => DateTime.UtcNow;

        public static DateTime UtcNow
        {
            get { return Current(); }
        }
    }
}
