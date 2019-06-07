using Serilog.Core;

namespace Catalyst.Cli
{
    internal static class AppLoggingLevelSwitch
    {
        private static LoggingLevelSwitch _loggingLevelSwitch;

        public static LoggingLevelSwitch LoggingLevelSwitch => _loggingLevelSwitch ?? (_loggingLevelSwitch = new LoggingLevelSwitch());
    }
}
