#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using Catalyst.Modules.Network.Dotnetty.IO.Codecs;
using Catalyst.Modules.Network.Dotnetty.IO.Handlers;
using Catalyst.Modules.Network.Dotnetty.IO.Transport.Channels;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using DotNetty.Buffers;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using MultiFormats;

namespace Catalyst.Core.Modules.Rpc.Client.IO.Transport.Channels
{
    public class RpcClientChannelFactory : TcpClientChannelFactory<IObserverDto<ProtocolMessage>>
    {
        private readonly IKeySigner _keySigner;
        private readonly IRpcMessageCorrelationManager _messageCorrelationCache;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IObservableServiceHandler<IObserverDto<ProtocolMessage>> _observableServiceHandler;
        private readonly SigningContext _signingContext;

        /// <summary>
        /// </summary>
        /// <param name="keySigner"></param>
        /// <param name="messageCorrelationCache"></param>
        /// <param name="peerIdValidator"></param>
        /// <param name="peerSettings"></param>
        /// <param name="backLogValue"></param>
        /// <param name="scheduler"></param>
        public RpcClientChannelFactory(IKeySigner keySigner,
            IRpcMessageCorrelationManager messageCorrelationCache,
            IPeerIdValidator peerIdValidator,
            IPeerSettings peerSettings,
            int backLogValue = 100,
            IScheduler scheduler = null) : base(backLogValue)
        {
            var observableScheduler = scheduler ?? Scheduler.Default;
            _keySigner = keySigner;
            _messageCorrelationCache = messageCorrelationCache;
            _peerIdValidator = peerIdValidator;
            _signingContext = new SigningContext {NetworkType = peerSettings.NetworkType, SignatureType = SignatureType.ProtocolRpc};
            _observableServiceHandler = new RpcObservableServiceHandler(observableScheduler);
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
                    new ProtobufDecoder(ProtocolMessage.Parser),
                    new PeerIdValidationHandler(_peerIdValidator),
                    new AddressedEnvelopeToIMessageEncoder(),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new ProtocolMessageVerifyHandler(_keySigner),
                        new ProtocolMessageSignHandler(_keySigner, _signingContext)),
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
        public override async Task<IObservableChannel<IObserverDto<ProtocolMessage>>> BuildChannelAsync(IEventLoopGroupFactory eventLoopGroupFactory,
            MultiAddress address,
            X509Certificate2 certificate = null)
        {
            var channel = await BootstrapAsync(eventLoopGroupFactory, address, certificate).ConfigureAwait(false);

            var messageStream = _observableServiceHandler.MessageStream;

            return new ObservableChannel<IObserverDto<ProtocolMessage>>(messageStream ?? Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
