using System;
using Catalyst.Common.Interfaces.Cli;
using Serilog.Core;
using Serilog.Events;

namespace Catalyst.Cli
{
    public class AppLoggingLevelSwitch
    {
        private static LoggingLevelSwitch _loggingLevelSwitch = null;

        private AppLoggingLevelSwitch() { }

        public static LoggingLevelSwitch LoggingLevelSwitch => _loggingLevelSwitch ?? (_loggingLevelSwitch = new LoggingLevelSwitch());
    }
}
