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
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.LibP2PHandlers;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Wire;
using Lib.P2P;
using Lib.P2P.Protocols;
using Lib.P2P.PubSub;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.P2P
{
    public class LibP2PPeerService : ILibP2PPeerService
    {
        private readonly IScheduler _scheduler;
        private readonly IPeerMessageCorrelationManager _messageCorrelationManager;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IPubSubApi _pubSubApi;
        private readonly ICatalystProtocol _catalystProtocol;
        private readonly Peer _localPeer;
        private readonly IList<IInboundMessageHandler> _handlers;
        private readonly IEnumerable<IP2PMessageObserver> _messageObservers;
        private readonly IPubSubService _pubSubService;
        private readonly ILogger _logger;

        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _messageSubject;
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { private set; get; }

        /// <param name="clientChannelFactory">A factory used to build the appropriate kind of channel for a udp client.</param>
        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="peerSettings"></param>
        public LibP2PPeerService(
            IEnumerable<IP2PMessageObserver> messageObservers,
            IPeerMessageCorrelationManager messageCorrelationManager,
            IKeySigner keySigner,
            IPeerIdValidator peerIdValidator,
            Peer localPeer,
            IPubSubApi pubSubApi,
            ICatalystProtocol catalystProtocol,
            IPubSubService pubSubService,
            ILogger logger,
            IScheduler scheduler = null
            )
        {
            _scheduler = scheduler ?? Scheduler.Default;
            _messageCorrelationManager = messageCorrelationManager;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
            _localPeer = localPeer;
            _pubSubApi = pubSubApi;
            _catalystProtocol = catalystProtocol;
            _pubSubService = pubSubService;
            _messageSubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(_scheduler);
            MessageStream = _messageSubject.AsObservable();

            _handlers = new List<IInboundMessageHandler>
            {
                new PeerIdValidationHandler(_peerIdValidator),
                new ProtocolMessageVerifyHandler(_keySigner),
                new CorrelationHandler<IPeerMessageCorrelationManager>(_messageCorrelationManager)
            };

            _messageObservers = messageObservers;
            _logger = logger;
        }


        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public async Task<IObservable<IObserverDto<ProtocolMessage>>> BuildMessageStreamAsync()
        {
            await SubscribeToCatalystLibP2PProtocol().ConfigureAwait(false);

            await SubscribeToCatalystPubSub().ConfigureAwait(false);

            return MessageStream;
        }

        private Task SubscribeToCatalystLibP2PProtocol()
        {
            _catalystProtocol.MessageStream.Subscribe(async message =>
            {
                await ProcessMessageAsync(message).ConfigureAwait(false);
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
                    await ProcessMessageAsync(protocolMessage).ConfigureAwait(false);
                }
            }, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task ProcessMessageAsync(ProtocolMessage message)
        {
            foreach (var handler in _handlers)
            {
                var result = await handler.ProcessAsync(message).ConfigureAwait(false);
                if (!result)
                {
                    return;
                }
            }

            _messageSubject.OnNext(new ObserverDto(null, message));
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            MessageStream = await BuildMessageStreamAsync().ConfigureAwait(false);
            _messageObservers.ToList().ForEach(h => h.StartObserving(MessageStream));

            foreach (var router in _pubSubService.Routers)
            {
                await router.JoinTopicAsync("catalyst", cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
