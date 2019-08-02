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

using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using DotNetty.Transport.Channels;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Catalyst.Common.IO.Transport
{
    public abstract class SocketBase : ISocket
    {
        protected readonly IChannelFactory ChannelFactory;
        private readonly ILogger _logger;
        private bool _disposing;
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
            if (_disposing)
            {
                return;
            }

            _disposing = true;
            if (!disposing)
            {
                return;
            }

            _logger.Debug($"Disposing{GetType().Name}");

            var quietPeriod = TimeSpan.FromMilliseconds(100);

            try
            {
                Channel?.Flush();
                var closeChannelTask = Channel?.CloseAsync();

                Task.WaitAll(new[] {closeChannelTask}.Where(t => t != null).ToArray(),
                    quietPeriod * 2);

                EventLoopGroupFactory.Dispose();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Dispose failed to complete.");
            }
        }
    }
}
