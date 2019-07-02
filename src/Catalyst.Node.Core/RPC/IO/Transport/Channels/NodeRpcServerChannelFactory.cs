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

using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Handlers;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.Authentication;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Core.RPC.IO.Transport.Channels
{
    public class NodeRpcServerChannelFactory : TcpServerChannelFactory
    {
        private readonly IMessageCorrelationManager _correlationManger;
        private readonly IAuthenticationStrategy _authenticationStrategy;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdValidator _peerIdValidator;

        protected override List<IChannelHandler> Handlers =>
            new List<IChannelHandler>
            {
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(ProtocolMessageSigned.Parser),
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new AuthenticationHandler(_authenticationStrategy),
                new PeerIdValidationHandler(_peerIdValidator),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                    new ProtocolMessageVerifyHandler(_keySigner), new ProtocolMessageSignHandler(_keySigner)
                ),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                    new CorrelationHandler(_correlationManger), new CorrelationHandler(_correlationManger)
                ),
                new ObservableServiceHandler()
            };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="correlationManger"></param>
        /// <param name="keySigner"></param>
        /// <param name="authenticationStrategy"></param>
        /// <param name="peerIdValidator"></param>
        /// <param name="logger"></param>
        public NodeRpcServerChannelFactory(IMessageCorrelationManager correlationManger,
            IKeySigner keySigner,
            IAuthenticationStrategy authenticationStrategy,
            IPeerIdValidator peerIdValidator)
        {
            _correlationManger = correlationManger;
            _authenticationStrategy = authenticationStrategy;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetPort"></param>
        /// <param name="certificate">Local TLS certificate</param>
        /// <param name="targetAddress"></param>
        public override IObservableChannel BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channel = Bootstrap(handlerEventLoopGroupFactory, targetAddress, targetPort, certificate);
            
            var messageStream = channel.Pipeline.Get<IObservableServiceHandler>()?.MessageStream;

            return new ObservableChannel(messageStream
             ?? Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
