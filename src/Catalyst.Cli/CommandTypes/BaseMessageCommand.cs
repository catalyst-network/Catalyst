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
using System.Collections.Concurrent;
using System.Reactive.Linq;
using Catalyst.Abstractions.Cli.Commands;
using Catalyst.Abstractions.Cli.CommandTypes;
using Catalyst.Abstractions.Cli.Options;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Events;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.P2P;
using Catalyst.Protocol;
using Google.Protobuf;

namespace Catalyst.Cli.CommandTypes
{
    public abstract class BaseMessageCommand<TRequest, TResponse, TOption> : BaseCommand<TOption>,
        IMessageCommand<TRequest>, IDisposable
        where TRequest : IMessage<TRequest>
        where TResponse : IMessage<TResponse>
        where TOption : IOptionsBase
    {
        private readonly IDisposable _eventStreamObserverClientAdded;
        private readonly IDisposable _eventStreamObserverClientRemoved;
        private readonly ConcurrentDictionary<int, IDisposable> _subscriptions;

        protected BaseMessageCommand(ICommandContext commandContext) : base(commandContext)
        {
            _subscriptions = new ConcurrentDictionary<int, IDisposable>();
            _eventStreamObserverClientAdded = CommandContext.SocketClientRegistry.EventStream
               .OfType<SocketClientRegistryClientAdded>().Subscribe(SocketClientRegistryClientAddedOnNext);
            _eventStreamObserverClientRemoved = CommandContext.SocketClientRegistry.EventStream
               .OfType<SocketClientRegistryClientRemoved>().Subscribe(SocketClientRegistryClientRemovedOnNext);
        }

        protected IPeerIdentifier RecipientPeerIdentifier =>
            PeerIdentifier.BuildPeerIdFromConfig(CommandContext.GetNodeConfig(Options.Node));

        protected IPeerIdentifier SenderPeerIdentifier => CommandContext.PeerIdentifier;

        public void Dispose()
        {
            Dispose(true);
        }

        public INodeRpcClient Target => CommandContext.GetConnectedNode(Options.Node);

        public virtual void SendMessage(TOption options)
        {
            var message = GetMessage(options);

            if (message == null)
            {
                return;
            }

            var messageDto = new MessageDto(
                message.ToProtocolMessage(SenderPeerIdentifier.PeerId),
                RecipientPeerIdentifier);
            Target.SendMessage(messageDto);
        }

        protected abstract TRequest GetMessage(TOption option);

        protected override bool ExecuteCommandInner(IOptionsBase optionsBase)
        {
            var sendMessage = base.ExecuteCommandInner(optionsBase);

            if (sendMessage)
            {
                SendMessage((TOption) optionsBase);
            }

            return sendMessage;
        }

        protected virtual void ResponseMessage(TResponse response)
        {
            CommandContext.UserOutput.WriteLine(response.ToJsonString());
        }

        private void CommandResponseOnNext(TResponse value) { ResponseMessage(value); }

        private void SocketClientRegistryClientAddedOnNext(SocketClientRegistryClientAdded clientAddedEvent)
        {
            var client = CommandContext.SocketClientRegistry.GetClientFromRegistry(clientAddedEvent.SocketHashCode);
            var subscription = client.SubscribeToResponse<TResponse>(CommandResponseOnNext);
            _subscriptions.TryAdd(clientAddedEvent.SocketHashCode, subscription);
        }

        private void SocketClientRegistryClientRemovedOnNext(SocketClientRegistryClientRemoved clientRemovedEvent)
        {
            var subscriptionRemoved = _subscriptions.TryRemove(clientRemovedEvent.SocketHashCode, out var outSubscription);
            if (subscriptionRemoved)
            {
                outSubscription.Dispose();
            }
        }

        private void DisposeSubscriptions()
        {
            foreach (var subscriptionPair in _subscriptions)
            {
                subscriptionPair.Value.Dispose();
            }

            _subscriptions.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _eventStreamObserverClientAdded?.Dispose();
            _eventStreamObserverClientRemoved?.Dispose();
            DisposeSubscriptions();
        }
    }
}
