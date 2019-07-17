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
using Catalyst.Common.Interfaces.IO.Handlers;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Codecs;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.IO.Transport.Channels
{
    public class PeerServerChannelFactory : UdpServerChannelFactory
    {
        private readonly IPeerMessageCorrelationManager _messageCorrelationManager;
        private readonly IBroadcastManager _broadcastManager;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdValidator _peerIdValidator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageCorrelationManager"></param>
        /// <param name="broadcastManager"></param>
        /// <param name="keySigner"></param>
        /// <param name="peerIdValidator"></param>
        public PeerServerChannelFactory(IPeerMessageCorrelationManager messageCorrelationManager,
            IBroadcastManager broadcastManager,
            IKeySigner keySigner,
            IPeerIdValidator peerIdValidator)
        {
            _messageCorrelationManager = messageCorrelationManager;
            _broadcastManager = broadcastManager;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
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
                            new DatagramPacketDecoder(new ProtobufDecoder(ProtocolMessageSigned.Parser)),
                            new DatagramPacketEncoder<IMessage>(new ProtobufEncoder())
                        ),
                        new PeerIdValidationHandler(_peerIdValidator),
                        new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                            new ProtocolMessageVerifyHandler(_keySigner),
                            new ProtocolMessageSignHandler(_keySigner)
                        ),
                        new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                            new CorrelationHandler<IPeerMessageCorrelationManager>(_messageCorrelationManager),
                            new CorrelatableHandler<IPeerMessageCorrelationManager>(_messageCorrelationManager)
                        ),
                        new BroadcastHandler(_broadcastManager),
                        new ObservableServiceHandler()
                    };
                };
            }
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public override IObservableChannel BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channel = BootStrapChannel(handlerEventLoopGroupFactory, targetAddress, targetPort);

            var messageStream = channel.Pipeline.Get<IObservableServiceHandler>()?.MessageStream;

            return new ObservableChannel(messageStream
             ?? Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
