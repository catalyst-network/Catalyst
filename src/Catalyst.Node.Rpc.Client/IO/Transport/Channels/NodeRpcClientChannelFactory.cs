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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Handlers;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Correlation;
using Catalyst.Common.IO.Codecs;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Rpc.Client.IO.Transport.Channels
{
    public class NodeRpcClientChannelFactory : TcpClientChannelFactory
    {
        private readonly IKeySigner _keySigner;
        private readonly IRpcMessageCorrelationManager _messageCorrelationCache;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IObservableServiceHandler _observableServiceHandler;
        private readonly ISigningContextProvider _signingContextProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keySigner"></param>
        /// <param name="messageCorrelationCache"></param>
        /// <param name="peerIdValidator"></param>
        /// <param name="signingContextProvider"></param>
        /// <param name="backLogValue"></param>
        public NodeRpcClientChannelFactory(IKeySigner keySigner,
            IRpcMessageCorrelationManager messageCorrelationCache,
            IPeerIdValidator peerIdValidator,
            ISigningContextProvider signingContextProvider,
            int backLogValue = 100) : base(backLogValue)
        {
            _keySigner = keySigner;
            _messageCorrelationCache = messageCorrelationCache;
            _peerIdValidator = peerIdValidator;
            _signingContextProvider = signingContextProvider;
            _observableServiceHandler = new ObservableServiceHandler();
        }

        protected override Func<List<IChannelHandler>> HandlerGenerationFunction
        {
            get
            {
                return () => new List<IChannelHandler>
                {
                    new FlushPipelineHandler<IByteBuffer>(),
                    new ProtobufVarint32LengthFieldPrepender(),
                    new ProtobufEncoder(),
                    new ProtobufVarint32FrameDecoder(),
                    new ProtobufDecoder(ProtocolMessageSigned.Parser),
                    new PeerIdValidationHandler(_peerIdValidator),
                    new AddressedEnvelopeToIMessageEncoder(),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new ProtocolMessageVerifyHandler(_keySigner, _signingContextProvider), 
                        new ProtocolMessageSignHandler(_keySigner, _signingContextProvider)),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new CorrelationHandler<IRpcMessageCorrelationManager>(_messageCorrelationCache),
                        new CorrelatableHandler<IRpcMessageCorrelationManager>(_messageCorrelationCache)),
                    _observableServiceHandler
                };
            }
        }

        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public override async Task<IObservableChannel> BuildChannel(IEventLoopGroupFactory eventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channel = await Bootstrap(eventLoopGroupFactory, targetAddress, targetPort, certificate);

            var messageStream = _observableServiceHandler.MessageStream;

            return new ObservableChannel(messageStream
             ?? Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
