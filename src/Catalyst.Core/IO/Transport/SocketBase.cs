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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Abstractions.IO.Transport.Channels;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.IO.Transport
{
    public abstract class SocketBase : ISocket
    {
        protected readonly IChannelFactory ChannelFactory;
        private readonly ILogger _logger;
        private int _disposeCounter;
        protected readonly IEventLoopGroupFactory EventLoopGroupFactory;
        public abstract Task StartAsync();

        public IChannel Channel { get; protected set; }

        protected SocketBase(IChannelFactory channelFactory, ILogger logger, IEventLoopGroupFactory eventLoopGroupFactory)
        {
            ChannelFactory = channelFactory;
            _logger = logger;
            EventLoopGroupFactory = eventLoopGroupFactory;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || Interlocked.Increment(ref _disposeCounter) > 1)
            {
                return;
            }
            
            _logger.Debug($"Disposing{GetType().Name}");

            var quietPeriod = TimeSpan.FromMilliseconds(100);

            try
            {
                Channel?.Flush();
                Channel?.CloseAsync().Wait(quietPeriod);
                EventLoopGroupFactory.Dispose();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Dispose failed to complete.");
            }
        }
    }
}
