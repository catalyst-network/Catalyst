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
using System.Collections.Immutable;
using System.Linq;
using Dawn;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO
{
    public class ChannelInitializerBase<T>
        : ChannelInitializer<T>
        where T : IChannel
    {
        private readonly Action<T> _initializationAction;
        private readonly IReadOnlyCollection<IChannelHandler> _handlers;
        private readonly TlsHandler _tlsHandler;

        protected ChannelInitializerBase(Action<T> initializationAction,
            IList<IChannelHandler> handlers,
            TlsHandler tlsHandler)
        {
            Guard.Argument(initializationAction, nameof(initializationAction)).NotNull();
            Guard.Argument(handlers, nameof(handlers)).NotNull().NotEmpty();
            _initializationAction = initializationAction;
            _handlers = handlers.ToImmutableArray();
            _tlsHandler = tlsHandler;
        }

        protected override void InitChannel(T channel)
        {
            _initializationAction(channel);
            var pipeline = channel.Pipeline;

            if (_tlsHandler != null)
            {
                pipeline.AddLast(_tlsHandler);
            }

            pipeline.AddLast(new LoggingHandler(LogLevel.TRACE));
            pipeline.AddLast(_handlers.ToArray());
        }
    }
}
