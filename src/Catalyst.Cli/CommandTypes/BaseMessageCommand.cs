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
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Events;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using Serilog;

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
        private PeerId _recipientPeerId;

        protected BaseMessageCommand(ICommandContext commandContext, ILogger logger)
            : base(commandContext, logger)
        {
            _subscriptions = new ConcurrentDictionary<int, IDisposable>();
            _eventStreamObserverClientAdded = CommandContext.SocketClientRegistry.EventStream
               .OfType<SocketClientRegistryClientAdded>().Subscribe(SocketClientRegistryClientAddedOnNext);
            _eventStreamObserverClientRemoved = CommandContext.SocketClientRegistry.EventStream
               .OfType<SocketClientRegistryClientRemoved>().Subscribe(SocketClientRegistryClientRemovedOnNext);
        }

        protected PeerId RecipientPeerId
        {
            get
            {
                if (_recipientPeerId != null) return _recipientPeerId;
                var rpcClientConfig = CommandContext.GetNodeConfig(Options.Node);
                _recipientPeerId =
                    rpcClientConfig.PublicKey.BuildPeerIdFromBase58Key(rpcClientConfig.HostAddress,
                        rpcClientConfig.Port);
                return _recipientPeerId;
            }
        }

        protected PeerId SenderPeerId => CommandContext.PeerId;

        public void Dispose() { Dispose(true); }

        public IRpcClient Target => CommandContext.GetConnectedNode(Options.Node);

        public virtual void SendMessage(TOption options)
        {
            var message = GetMessage(options);

            if (message == null) return;

            var messageDto = new MessageDto(
                message.ToProtocolMessage(SenderPeerId),
                RecipientPeerId);
            Target.SendMessage(messageDto);
        }

        protected abstract TRequest GetMessage(TOption option);

        protected override bool ExecuteCommandInner(IOptionsBase optionsBase)
        {
            var sendMessage = base.ExecuteCommandInner(optionsBase);

            if (sendMessage) SendMessage((TOption) optionsBase);

            return sendMessage;
        }

        protected virtual void ResponseMessage(TResponse response)
        {
            CommandContext.UserOutput.WriteLine(response.ToJsonString());
        }

        private void CommandResponseOnNext(TResponse value) { ResponseMessage(value); }

        private void SocketClientRegistryClientAddedOnNext(SocketClientRegistryClientAdded clientAddedEvent)
        {
            try
            {
                var client = CommandContext.SocketClientRegistry.GetClientFromRegistry(clientAddedEvent.SocketHashCode);
                var subscription = client.SubscribeToResponse<TResponse>(CommandResponseOnNext);
                _subscriptions.TryAdd(clientAddedEvent.SocketHashCode, subscription);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error subscribing to Rpc response stream.");
            }
        }

        private void SocketClientRegistryClientRemovedOnNext(SocketClientRegistryClientRemoved clientRemovedEvent)
        {
            var subscriptionRemoved =
                _subscriptions.TryRemove(clientRemovedEvent.SocketHashCode, out var outSubscription);
            if (subscriptionRemoved) outSubscription.Dispose();
        }

        private void DisposeSubscriptions()
        {
            foreach (var subscriptionPair in _subscriptions) subscriptionPair.Value.Dispose();

            _subscriptions.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _eventStreamObserverClientAdded?.Dispose();
            _eventStreamObserverClientRemoved?.Dispose();
            DisposeSubscriptions();
        }
    }
}
