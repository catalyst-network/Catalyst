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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Core.IO.Handlers;
using Catalyst.Core.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;

namespace Catalyst.Core.P2P.IO.Transport.Channels
{
    public class PeerClientChannelFactory : UdpClientChannelFactory
    {
        private readonly IScheduler _scheduler;
        private readonly IKeySigner _keySigner;
        private readonly IPeerMessageCorrelationManager _correlationManager;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly ISigningContextProvider _signingContextProvider;

        protected override Func<List<IChannelHandler>> HandlerGenerationFunction
        {
            get
            {
                return () => new List<IChannelHandler>
                {
                    new FlushPipelineHandler<DatagramPacket>(),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new DatagramPacketDecoder(new ProtobufDecoder(ProtocolMessageSigned.Parser)),
                        new DatagramPacketEncoder<IMessage>(new ProtobufEncoder())
                    ),
                    new PeerIdValidationHandler(_peerIdValidator),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new ProtocolMessageVerifyHandler(_keySigner, _signingContextProvider),
                        new ProtocolMessageSignHandler(_keySigner, _signingContextProvider)
                    ),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new CorrelationHandler<IPeerMessageCorrelationManager>(_correlationManager),
                        new CorrelatableHandler<IPeerMessageCorrelationManager>(_correlationManager)
                    ),
                    new ObservableServiceHandler(_scheduler)
                };
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="keySigner"></param>
        /// <param name="correlationManager"></param>
        /// <param name="peerIdValidator"></param>
        /// <param name="signingContextProvider"></param>
        /// <param name="scheduler"></param>
        public PeerClientChannelFactory(IKeySigner keySigner,
            IPeerMessageCorrelationManager correlationManager,
            IPeerIdValidator peerIdValidator,
            ISigningContextProvider signingContextProvider,
            IScheduler scheduler = null)
        {
            _scheduler = scheduler ?? Scheduler.Default;
            _keySigner = keySigner;
            _correlationManager = correlationManager;
            _peerIdValidator = peerIdValidator;
            _signingContextProvider = signingContextProvider;
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public override async Task<IObservableChannel> BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channel = await BootStrapChannel(handlerEventLoopGroupFactory, targetAddress, targetPort);
            return new ObservableChannel(Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
