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

using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli.Commands;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.P2P;
using Google.Protobuf;
using System;

namespace Catalyst.Cli.Commands
{
    public abstract class MessageCommand<T, TOption> : CommandBase, IMessageCommand<T>
        where T : IMessage<T>
        where TOption : IOptionsBase
    {
        protected MessageCommand(ICommandContext commandContext)
        {
            CommandContext = commandContext;
        }

        public virtual void SendMessage(TOption options)
        {
            var message = GetMessage(options);

            if (message != null)
            {
                var messageDto = CommandContext.DtoFactory.GetDto(
                    message.ToProtocolMessage(SenderPeerIdentifier.PeerId),
                    SenderPeerIdentifier,
                    RecipientPeerIdentifier);
                Target.SendMessage(messageDto);
            }
        }

        protected abstract T GetMessage(TOption option);

        protected ICommandContext CommandContext { get; }

        protected IPeerIdentifier RecipientPeerIdentifier => PeerIdentifier.BuildPeerIdFromConfig(CommandContext.GetNodeConfig(Options.Node), CommandContext.PeerIdClientId);

        protected IPeerIdentifier SenderPeerIdentifier => CommandContext.PeerIdentifier;

        public INodeRpcClient Target => CommandContext.GetConnectedNode(Options.Node);

        protected IOptionsBase Options { get; set; }

        protected override void ExecuteCommand(IOptionsBase optionsBase)
        {
            Options = optionsBase;
            SendMessage((TOption) optionsBase);
        }

        public override Type OptionType => typeof(TOption);
    }
}
