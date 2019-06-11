#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System;
using Catalyst.Common.Interfaces.Cli;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Catalyst.Common.Shell
{
    /// <summary>
    /// Handles CLI application minimum logging level
    /// </summary>
    /// <seealso cref="IAppLoggingLevelSwitch" />
    public sealed class AppLoggingLevelSwitch : IAppLoggingLevelSwitch
    {
        private readonly IConfigurationRoot _configurationRoot;

        /// <summary>Initializes a new instance of the <see cref="AppLoggingLevelSwitch"/> class.</summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        public AppLoggingLevelSwitch(IConfigurationRoot configuration, LoggerConfiguration loggerConfiguration)
        {
            _configurationRoot = configuration;
            Configure(loggerConfiguration);
        }

        /// <summary>Configures the specified logger.</summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        private void Configure(LoggerConfiguration loggerConfiguration)
        {
            var minimumLevel = Enum.Parse<LogEventLevel>(_configurationRoot["Serilog:MinimumLevel"]);
            LoggingLevelSwitch = new LoggingLevelSwitch(minimumLevel);
            loggerConfiguration.MinimumLevel.ControlledBy(LoggingLevelSwitch);
        }

        /// <summary>Gets the logging level switch.</summary>
        /// <value>The logging level switch.</value>
        public LoggingLevelSwitch LoggingLevelSwitch { get; private set; }
    }
}
