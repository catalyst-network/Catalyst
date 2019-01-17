using System;

namespace Catalyst.Helpers.Util
{
    public static class DateTimeProvider
    {
        public static DateTime UtcNow => Current();
        private static readonly Func<DateTime> Current = () => DateTime.UtcNow;
    }
}
