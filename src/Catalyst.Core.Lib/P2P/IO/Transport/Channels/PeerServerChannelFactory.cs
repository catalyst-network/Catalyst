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
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.IO.Transport.Channels;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using LibP2P = Lib.P2P;

namespace Catalyst.Core.Lib.P2P.IO.Transport.Channels
{
    public class PeerServerChannelFactory : UdpServerChannelFactory
    {
        private readonly IScheduler _scheduler;
        private readonly IPeerMessageCorrelationManager _messageCorrelationManager;
        private readonly IBroadcastManager _broadcastManager;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IPubSubApi _pubSubApi;
        private readonly SigningContext _signingContext;
        private readonly LibP2P.Peer _localPeer;
        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _messageSubject;
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; }

        public PeerServerChannelFactory(IPeerMessageCorrelationManager messageCorrelationManager,
            LibP2P.Peer localPeer,
            IPubSubApi pubSubApi,
            IBroadcastManager broadcastManager,
            IKeySigner keySigner,
            IPeerIdValidator peerIdValidator,
            IPeerSettings peerSettings,
            IScheduler scheduler = null)
        {
            _localPeer = localPeer;
            _pubSubApi = pubSubApi;
            _scheduler = scheduler ?? Scheduler.Default;
            _messageCorrelationManager = messageCorrelationManager;
            _broadcastManager = broadcastManager;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
            _signingContext = new SigningContext {NetworkType = peerSettings.NetworkType, SignatureType = SignatureType.ProtocolPeer};
            _messageSubject = new ReplaySubject<IObserverDto<ProtocolMessage>>();
            MessageStream = _messageSubject.AsObservable();
        }

        protected override Func<List<IChannelHandler>> HandlerGenerationFunction
        {
            get
            {
                return () =>
                {
                    return new List<IChannelHandler>
                    {
                        new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                            new DatagramPacketDecoder(new ProtobufDecoder(ProtocolMessage.Parser)),
                            new DatagramPacketEncoder<IMessage>(new ProtobufEncoder())
                        ),
                        new PeerIdValidationHandler(_peerIdValidator),
                        //new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        //    new ProtocolMessageVerifyHandler(_keySigner),
                        //    new ProtocolMessageSignHandler(_keySigner, _signingContext)
                        //),
                        new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                            new CorrelationHandler<IPeerMessageCorrelationManager>(_messageCorrelationManager),
                            new CorrelatableHandler<IPeerMessageCorrelationManager>(_messageCorrelationManager)
                        ),
                        new BroadcastHandler(_broadcastManager),
                        new ObservableServiceHandler(_scheduler),
                        new BroadcastCleanupHandler(_broadcastManager)
                    };
                };
            }
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public override async Task<IObservableChannel> BuildChannelAsync(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channel = await BootStrapChannelAsync(handlerEventLoopGroupFactory, targetAddress, targetPort).ConfigureAwait(false);

            var messageStream = channel.Pipeline.Get<IObservableServiceHandler>()?.MessageStream;

            await _pubSubApi.SubscribeAsync("catalyst", msg =>
            {
                if (msg.Sender.Id != _localPeer.Id)
                {
                    var proto = ProtocolMessage.Parser.ParseFrom(msg.DataStream);
                    if (proto.IsBroadCastMessage())
                    {
                        var innerGossipMessageSigned = ProtocolMessage.Parser.ParseFrom(proto.Value);
                        _messageSubject.OnNext(new ObserverDto(null, innerGossipMessageSigned));
                        return;
                    }

                    _messageSubject.OnNext(new ObserverDto(null, proto));
                }
            }, CancellationToken.None);

            return new ObservableChannel(MessageStream
             ?? Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
