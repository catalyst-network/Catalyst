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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.IO.Transport.Channels;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;
using MultiFormats;

namespace Catalyst.Core.Lib.P2P.IO.Transport.Channels
{
    public class PeerClientChannelFactory : UdpClientChannelFactory
    {
        private readonly IScheduler _scheduler;
        private readonly IKeySigner _keySigner;
        private readonly IPeerMessageCorrelationManager _correlationManager;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly SigningContext _signingContext;

        protected override Func<List<IChannelHandler>> HandlerGenerationFunction
        {
            get
            {
                return () => new List<IChannelHandler>
                {
                    new FlushPipelineHandler<DatagramPacket>(),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new DatagramPacketDecoder(new ProtobufDecoder(ProtocolMessage.Parser)),
                        new DatagramPacketEncoder<IMessage>(new ProtobufEncoder())
                    ),
                    new PeerIdValidationHandler(_peerIdValidator),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new ProtocolMessageVerifyHandler(_keySigner),
                        new ProtocolMessageSignHandler(_keySigner, _signingContext)
                    ),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new CorrelationHandler<IPeerMessageCorrelationManager>(_correlationManager),
                        new CorrelatableHandler<IPeerMessageCorrelationManager>(_correlationManager)
                    ),
                    new ObservableServiceHandler(_scheduler)
                };
            }
        }

        public PeerClientChannelFactory(IKeySigner keySigner,
            IPeerMessageCorrelationManager correlationManager,
            IPeerIdValidator peerIdValidator,
            IPeerSettings peerSettings,
            IScheduler scheduler = null)
        {
            _scheduler = scheduler ?? Scheduler.Default;
            _keySigner = keySigner;
            _correlationManager = correlationManager;
            _peerIdValidator = peerIdValidator;
            _signingContext = new SigningContext {NetworkType = peerSettings.NetworkType, SignatureType = SignatureType.ProtocolPeer};
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public override async Task<IObservableChannel> BuildChannelAsync(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            MultiAddress address,
            X509Certificate2 certificate = null)
        {
            var channel = await BootStrapChannelAsync(handlerEventLoopGroupFactory, address.GetIpAddress(), address.GetPort()).ConfigureAwait(false);
            return new ObservableChannel(Observable.Never<ProtocolMessage>(), channel);
        }
    }
}
