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

using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.LibP2PHandlers;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using Lib.P2P;
using Lib.P2P.Protocols;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.P2P.IO.Transport.Channels
{
    public class PeerLibP2PServerChannelFactory
    {
        private readonly IScheduler _scheduler;
        private readonly IPeerMessageCorrelationManager _messageCorrelationManager;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly SigningContext _signingContext;
        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _messageSubject;
        private readonly IPubSubApi _pubSubApi;
        private readonly ICatalystProtocol _catalystProtocol;
        private readonly Peer _localPeer;
        private readonly IList<IMessageHandler> _handlers;
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; }

        public PeerLibP2PServerChannelFactory(
            IPeerMessageCorrelationManager messageCorrelationManager,
            IKeySigner keySigner,
            IPeerIdValidator peerIdValidator,
            IPeerSettings peerSettings,
            Peer localPeer,
            IPubSubApi pubSubApi,
            ICatalystProtocol catalystProtocol,
            IScheduler scheduler = null)
        {
            _scheduler = scheduler ?? Scheduler.Default;
            _messageCorrelationManager = messageCorrelationManager;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
            _signingContext = new SigningContext { NetworkType = peerSettings.NetworkType, SignatureType = SignatureType.ProtocolPeer };
            _localPeer = localPeer;
            _pubSubApi = pubSubApi;
            _catalystProtocol = catalystProtocol;
            _messageSubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(_scheduler);
            MessageStream = _messageSubject.AsObservable();

            _handlers = new List<IMessageHandler>
            {
                new PeerIdValidationHandler(_peerIdValidator),
                new ProtocolMessageVerifyHandler(_keySigner),
                new CorrelationHandler<IPeerMessageCorrelationManager>(_messageCorrelationManager)
            };
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public async Task<IObservable<IObserverDto<ProtocolMessage>>> BuildMessageStreamAsync()
        {
            await SubscribeToCatalystLibP2PProtocol();

            await SubscribeToCatalystPubSub();

            return MessageStream;
        }

        private Task SubscribeToCatalystLibP2PProtocol()
        {
            _catalystProtocol.MessageStream.Subscribe(async message =>
            {
                await ProcessMessageAsync(message);
            });

            return Task.CompletedTask;
        }

        private async Task SubscribeToCatalystPubSub()
        {
            await _pubSubApi.SubscribeAsync("catalyst", async msg =>
            {
                if (msg.Sender.Id != _localPeer.Id)
                {
                    var protocolMessage = ProtocolMessage.Parser.ParseFrom(msg.DataStream);
                    await ProcessMessageAsync(UnwrapBroadcast(protocolMessage));
                }
            }, CancellationToken.None);
        }

        private ProtocolMessage UnwrapBroadcast(ProtocolMessage message)
        {
            if (message.IsBroadCastMessage())
            {
                var innerGossipMessageSigned = ProtocolMessage.Parser.ParseFrom(message.Value);
                return innerGossipMessageSigned; 
            }

            return message;
        }

        private async Task ProcessMessageAsync(ProtocolMessage message)
        {
            foreach (var handler in _handlers)
            {
                var result = await handler.ProcessAsync(message);
                if (!result)
                {
                    return;
                }
            }

            _messageSubject.OnNext(new ObserverDto(null, message));
        }
    }
}
