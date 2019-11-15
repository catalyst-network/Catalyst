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
using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Abstractions.Rpc.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Codecs;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.IO.Transport.Channels;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;

namespace Catalyst.Core.Modules.Rpc.Server.Transport.Channels
{
    public class RpcServerChannelFactory : TcpServerChannelFactory
    {
        private readonly IRpcMessageCorrelationManager _correlationManger;
        private readonly IAuthenticationStrategy _authenticationStrategy;
        private readonly IKeySigner _keySigner;
        private readonly IPeerIdValidator _peerIdValidator;
        private readonly IObservableServiceHandler _observableServiceHandler;
        private readonly SigningContext _signingContext;

        protected override Func<List<IChannelHandler>> HandlerGenerationFunction
        {
            get
            {
                return () => new List<IChannelHandler>
                {
                    new ProtobufVarint32FrameDecoder(),
                    new ProtobufDecoder(ProtocolMessage.Parser),
                    new ProtobufVarint32LengthFieldPrepender(),
                    new ProtobufEncoder(),
                    new AuthenticationHandler(_authenticationStrategy),
                    new PeerIdValidationHandler(_peerIdValidator),
                    new AddressedEnvelopeToIMessageEncoder(),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new ProtocolMessageVerifyHandler(_keySigner),
                        new ProtocolMessageSignHandler(_keySigner, _signingContext)
                    ),
                    new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(
                        new CorrelationHandler<IRpcMessageCorrelationManager>(_correlationManger),
                        new CorrelatableHandler<IRpcMessageCorrelationManager>(_correlationManger)
                    ),
                    _observableServiceHandler
                };
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="correlationManger"></param>
        /// <param name="keySigner"></param>
        /// <param name="authenticationStrategy"></param>
        /// <param name="peerIdValidator"></param>
        /// <param name="peerSettings"></param>
        /// <param name="scheduler"></param>
        public RpcServerChannelFactory(IRpcMessageCorrelationManager correlationManger,
            IKeySigner keySigner,
            IAuthenticationStrategy authenticationStrategy,
            IPeerIdValidator peerIdValidator,
            IPeerSettings peerSettings,
            IScheduler scheduler = null)
        {
            var observableScheduler = scheduler ?? Scheduler.Default;
            _correlationManger = correlationManger;
            _authenticationStrategy = authenticationStrategy;
            _keySigner = keySigner;
            _peerIdValidator = peerIdValidator;
            _signingContext = new SigningContext {NetworkType = peerSettings.NetworkType, SignatureType = SignatureType.ProtocolRpc};
            _observableServiceHandler = new ObservableServiceHandler(observableScheduler);
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetPort"></param>
        /// <param name="certificate">Local TLS certificate</param>
        /// <param name="targetAddress"></param>
        public override async Task<IObservableChannel> BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channel = await BootstrapAsync(handlerEventLoopGroupFactory, targetAddress, targetPort, certificate);

            var messageStream = _observableServiceHandler.MessageStream;

            return new ObservableChannel(messageStream
             ?? Observable.Never<IObserverDto<ProtocolMessage>>(), channel);
        }
    }
}
