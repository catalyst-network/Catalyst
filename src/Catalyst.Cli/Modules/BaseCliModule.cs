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
using Catalyst.Common.Interfaces.Cli;
using CommandLine;

namespace Catalyst.Cli.Modules
{
    public class BaseCliModule : ICliModule
    {
        private List<ICliCommand> _commands;
        private List<Type> _optionTypes;
        private readonly Parser _parser;

        public BaseCliModule(ICliCommand[] commands)
        {
            _parser = new Parser((settings => settings.IgnoreUnknownArguments = false));
            BuildModule(commands);
        }

        private void BuildModule(ICliCommand[] fullCommandList)
        {
            _commands = fullCommandList
               .Where(cmd =>
                    ((CliModuleAttribute) Attribute.GetCustomAttribute(cmd.GetType(), typeof(CliModuleAttribute)))
                   .GetModuleType() == this.GetType()).ToList();
            _optionTypes = _commands.Select(cmd =>
                ((CliOptionAttribute) Attribute.GetCustomAttribute(cmd.GetType(), typeof(CliOptionAttribute)))
               .GetOptionType()).ToList();
        }

        public bool HandleCommand(params string[] args)
        {
            var parserResult = _parser.ParseArguments(args, _optionTypes.ToArray());
            if (parserResult.Tag == ParserResultType.NotParsed)
            {
                return false;
            }

            int typeIdx = _optionTypes.ToList().IndexOf(parserResult.TypeInfo.Current);
            _commands[typeIdx].HandleCommand(args);

            return true;
        }
    }
}
