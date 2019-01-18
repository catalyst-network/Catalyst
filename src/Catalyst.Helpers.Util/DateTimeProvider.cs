using System;

namespace Catalyst.Helpers.Util
{
    public static class DateTimeProvider
    {
        private static readonly Func<DateTime> Current = () => DateTime.UtcNow;
        public static DateTime UtcNow => Current();
    }
}