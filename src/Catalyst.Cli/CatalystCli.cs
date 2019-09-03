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
using System.Collections.Generic;
using System.Linq;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Cli.CommandTypes;
using CommandLine;

namespace Catalyst.Cli
{
    /// <inheritdoc cref="CatalystCliBase" />
    public sealed class CatalystCli : CatalystCliBase
    {
        private readonly IEnumerable<ICommand> _commands;

        public CatalystCli(IUserOutput userOutput, IEnumerable<ICommand> commands) : base(userOutput)
        {
            _commands = commands;
            userOutput.WriteLine(@"Koopa Shell Start");
        }

        /// <inheritdoc cref="ParseCommand" />
        public override bool ParseCommand(params string[] args)
        {
            var parsedCommand = _commands.FirstOrDefault(command => command.CommandName.Equals(args[0], StringComparison.InvariantCultureIgnoreCase));
            var commandExists = parsedCommand != null;

            if (commandExists)
            {
                return parsedCommand.Parse(args);
            }

            var types = _commands.Select(command => command.OptionType).ToArray();
            Parser.Default.ParseArguments(args, types);

            return false;
        }
    }
}
