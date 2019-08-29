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
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Abstractions.IO.EventLoop;
using Dawn;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

namespace Catalyst.Core.IO.Transport.Channels
{
    public abstract class ChannelInitializerBase<T>
        : ChannelInitializer<T>
        where T : IChannel
    {
        private readonly IEventLoopGroupFactory _eventLoopGroupFactory;
        private readonly IPAddress _targetHost;
        private readonly X509Certificate _certificate;
        private readonly Func<IList<IChannelHandler>> _handlerGenerationFunction;

        protected ChannelInitializerBase(Func<IList<IChannelHandler>> handlerGenerationFunction,
            IEventLoopGroupFactory eventLoopGroupFactory,
            IPAddress targetHost = default,
            X509Certificate certificate = null)
        {
            Guard.Argument(handlerGenerationFunction).NotNull();
            _handlerGenerationFunction = handlerGenerationFunction;
            _eventLoopGroupFactory = eventLoopGroupFactory;
            _targetHost = targetHost;
            _certificate = certificate;
        }

        protected override void InitChannel(T channel)
        {
            var pipeline = channel.Pipeline;
            var tlsHandler = NewTlsHandler(_targetHost, _certificate);

            if (tlsHandler != null)
            {
                pipeline.AddLast(tlsHandler);
            }

            pipeline.AddLast(new LoggingHandler(LogLevel.TRACE));

            pipeline.AddLast(_eventLoopGroupFactory.GetOrCreateHandlerWorkerEventLoopGroup(), _handlerGenerationFunction().ToArray());
        }

        /// <summary>Creates a new TlsHandler.</summary>
        /// <returns><see cref="TlsHandler"/></returns>
        public abstract TlsHandler NewTlsHandler(IPAddress targetHost, X509Certificate certificate);
    }
}
