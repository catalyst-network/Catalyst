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

using Catalyst.Common.Interfaces.Cli.Options;
using CommandLine;
using System;
using System.Reflection;
using Catalyst.Common.Interfaces.Cli.CommandTypes;

namespace Catalyst.Cli.CommandTypes
{
    public abstract class BaseCommand : ICommand
    {
        protected abstract bool ExecuteCommandInner(IOptionsBase optionsBase);
        public abstract Type OptionType { get; }
        public string CommandName { get; }

        protected BaseCommand()
        {
            CommandName = ((VerbAttribute) OptionType.GetCustomAttribute(typeof(VerbAttribute))).Name;
        }

        public bool Parse(string[] args)
        {
            var parsedCommand = Parser.Default.ParseArguments(args, OptionType);
            if (parsedCommand.Tag == ParserResultType.NotParsed)
            {
                return false;
            }

            var result = false;
            parsedCommand.WithParsed(options => result = ExecuteCommandInner((IOptionsBase) options));
            return result;
        }
    }
}
