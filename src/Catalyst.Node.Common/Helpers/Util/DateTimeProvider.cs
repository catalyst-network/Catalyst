using System;

namespace Catalyst.Node.Common.Helpers.Util
{
    public static class DateTimeProvider
    {
        private static readonly Func<DateTime> Current = () => DateTime.UtcNow;
        public static DateTime UtcNow => Current();
    }
}