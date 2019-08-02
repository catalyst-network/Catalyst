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
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;

namespace Catalyst.Core.Lib.P2P.IO.Transport.Channels
{
    public class PeerClientChannelFactory : UdpClientChannelFactory
    {
        private readonly IKeySigner _keySigner;
        private readonly IPeerMessageCorrelationManager _correlationManager;
        private readonly IPeerIdValidator _peerIdValidator;

        protected override Func<List<IChannelHandler>> HandlerGenerationFunction
        {
            get
            {
                return () =>
                {
                    return new List<IChannelHandler>
                    {
                        new FlushPipelineHandler<DatagramPacket>(),
                        new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                            new DatagramPacketDecoder(new ProtobufDecoder(ProtocolMessageSigned.Parser)),
                            new DatagramPacketEncoder<IMessage>(new ProtobufEncoder())
                        ),
                        new PeerIdValidationHandler(_peerIdValidator),
                        new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                            new ProtocolMessageVerifyHandler(_keySigner),
                            new ProtocolMessageSignHandler(_keySigner)
                        ),
                        new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                            new CorrelationHandler<IPeerMessageCorrelationManager>(_correlationManager),
                            new CorrelatableHandler<IPeerMessageCorrelationManager>(_correlationManager)
                        ),
                        new ObservableServiceHandler()
                    };
                };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keySigner"></param>
        /// <param name="correlationManager"></param>
        /// <param name="peerIdValidator"></param>
        public PeerClientChannelFactory(IKeySigner keySigner,
            IPeerMessageCorrelationManager correlationManager,
            IPeerIdValidator peerIdValidator)
        {
            _keySigner = keySigner;
            _correlationManager = correlationManager;
            _peerIdValidator = peerIdValidator;
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public override IObservableChannel BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            Console.WriteLine("Bootstrapping Peer Client to " + targetAddress + " Port: " + targetPort);
            var channel = BootStrapChannel(handlerEventLoopGroupFactory, targetAddress, targetPort);

            Console.WriteLine("Finish Bootstrapping Peer Client");
            return new ObservableChannel(Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
