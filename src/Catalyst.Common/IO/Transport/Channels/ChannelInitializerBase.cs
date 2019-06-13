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
using System.Collections.Immutable;
using System.Linq;
using Dawn;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Transport.Channels
{
    public class ChannelInitializerBase<T>
        : ChannelInitializer<T>
        where T : IChannel
    {
        private readonly IImmutableList<IChannelHandler> _handlers;
        private readonly TlsHandler _tlsHandler;
        private readonly IEventLoopGroup _handlerEventLoopGroup;

        protected ChannelInitializerBase(IList<IChannelHandler> handlers,
            TlsHandler tlsHandler, 
            IEventLoopGroup handlerEventLoopGroup)
        {
            Guard.Argument(handlers, nameof(handlers)).NotNull().NotEmpty();
            _handlers = handlers.ToImmutableList();
            _handlerEventLoopGroup = handlerEventLoopGroup;
            _tlsHandler = tlsHandler;
        }

        protected override void InitChannel(T channel)
        {
            var pipeline = channel.Pipeline;

            if (_tlsHandler != null)
            {
                pipeline.AddLast(_tlsHandler);
            }

            pipeline.AddLast(new LoggingHandler(LogLevel.TRACE));

            if (_handlerEventLoopGroup == null)
            {
                pipeline.AddLast(_handlers.ToArray());
            }
            else
            {
                pipeline.AddLast(_handlerEventLoopGroup, _handlers.ToArray());
            }
        }
    }
}
