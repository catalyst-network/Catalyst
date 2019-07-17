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
using Catalyst.Common.Interfaces.Cli.CommandTypes;
using Catalyst.Common.Interfaces.Cli.Options;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Dto;
using Catalyst.Common.IO.Events;
using Catalyst.Common.P2P;
using Catalyst.Protocol;
using Google.Protobuf;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Catalyst.Cli.CommandTypes
{
    public abstract class BaseMessageCommand<TRequest, TResponse, TOption> : BaseCommand<TOption>, IMessageCommand<TRequest>
        where TRequest : IMessage<TRequest>
        where TResponse : IMessage<TResponse>
        where TOption : IOptionsBase
    {

        protected BaseMessageCommand(ICommandContext commandContext) : base(commandContext)
        {
            CommandContext.SocketClientRegistry.EventStream.OfType<SocketClientRegistryClientAdded>().Subscribe(SocketClientRegistryClientAddedOnNext);
        }

        public virtual void SendMessage(TOption options)
        {
            var message = GetMessage(options);

            if (message == null)
            {
                return;
            }

            var messageDto = CommandContext.DtoFactory.GetDto(
                message.ToProtocolMessage(SenderPeerIdentifier.PeerId),
                SenderPeerIdentifier,
                RecipientPeerIdentifier);
            Target.SendMessage(messageDto);
        }

        protected abstract TRequest GetMessage(TOption option);

        protected IPeerIdentifier RecipientPeerIdentifier => PeerIdentifier.BuildPeerIdFromConfig(CommandContext.GetNodeConfig(Options.Node), CommandContext.PeerIdClientId);

        protected IPeerIdentifier SenderPeerIdentifier => CommandContext.PeerIdentifier;

        public INodeRpcClient Target => CommandContext.GetConnectedNode(Options.Node);

        protected override bool ExecuteCommandInner(IOptionsBase optionsBase)
        {
            var sendMessage = base.ExecuteCommandInner(optionsBase);

            if (sendMessage)
            {
                SendMessage((TOption)optionsBase);
            }

            return sendMessage;
        }

        protected virtual void ResponseMessage(TResponse response)
        {
            CommandContext.UserOutput.WriteLine($"Response: {typeof(TResponse).ToString()}");
            CommandContext.UserOutput.WriteLine($"{response.ToJsonString()}");
        }

        private void CommandResponseOnNext(TResponse value)
        {
            ResponseMessage(value);
        }

        private void SocketClientRegistryClientAddedOnNext(SocketClientRegistryClientAdded value)
        {
            INodeRpcClient client = CommandContext.SocketClientRegistry.GetClientFromRegistry(value.SocketHashCode);
            client.Subscribe<TResponse>(CommandResponseOnNext);
        }
    }
}
