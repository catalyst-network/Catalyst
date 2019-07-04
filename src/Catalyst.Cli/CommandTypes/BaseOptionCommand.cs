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
using System;
using Catalyst.Common.Interfaces.Cli.Commands;

namespace Catalyst.Cli.CommandTypes
{
    public class BaseOptionCommand<TOption> : BaseCommand
        where TOption : IOptionsBase
    {
        public BaseOptionCommand(ICommandContext commandContext) { CommandContext = commandContext; }

        protected ICommandContext CommandContext { get; }

        protected IOptionsBase Options { get; set; }

        protected virtual bool ExecuteCommand(TOption option) { return true; }

        protected override bool ExecuteCommandInner(IOptionsBase optionsBase)
        {
            Options = optionsBase;
            return ExecuteCommand((TOption) Options);
        }

        public override Type OptionType => typeof(TOption);
    }
}
