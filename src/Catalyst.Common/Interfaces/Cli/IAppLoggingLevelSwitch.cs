using Serilog.Core;

namespace Catalyst.Common.Interfaces.Cli
{
    public interface IAppLoggingLevelSwitch
    {
        LoggingLevelSwitch LogLevelSwitch { get; }
    }
}
