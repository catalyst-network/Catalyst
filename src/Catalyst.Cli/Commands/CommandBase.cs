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

using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Interfaces.Cli.Options;
using CommandLine;
using System;
using System.Reflection;

namespace Catalyst.Cli.Commands
{
    public abstract class CommandBase : ICommand
    {
        protected abstract void ExecuteCommand(IOptionsBase optionsBase);
        public abstract Type OptionType { get; }
        public string CommandName { get; }

        protected CommandBase()
        {
            CommandName = ((VerbAttribute) OptionType.GetCustomAttribute(typeof(VerbAttribute))).Name;
        }

        public void Parse(string[] args)
        {
            var result = Parser.Default.ParseArguments(args, OptionType);
            if (result.Tag == ParserResultType.NotParsed)
            {
                return;
            }

            result.WithParsed(options => ExecuteCommand((IOptionsBase) options));
        }
    }
}
