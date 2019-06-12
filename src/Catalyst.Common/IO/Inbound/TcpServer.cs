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
using Catalyst.Common.Interfaces.IO.Inbound;
using DotNetty.Transport.Channels;
using Serilog;
using System;
using Catalyst.Common.Interfaces.IO;

namespace Catalyst.Common.IO.Inbound
{
    public class TcpServer : SocketBase, ITcpServer
    {
        private readonly IEventLoopGroup _supervisorEventLoop;

        protected TcpServer(ITcpServerChannelFactory tcpChannelFactory,
            ILogger logger,
            IHandlerWorkerEventLoopGroupFactory handlerWorkerEventLoopGroupFactory)
            : base(tcpChannelFactory, logger, handlerWorkerEventLoopGroupFactory.NewTcpServerLoopGroup())
        {
            _supervisorEventLoop = new MultithreadEventLoopGroup();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(true);
            if (!disposing)
            {
                return;
            }

            if (_supervisorEventLoop == null)
            {
                return;
            }

            var quietPeriod = TimeSpan.FromMilliseconds(100);
            _supervisorEventLoop
               .ShutdownGracefullyAsync(quietPeriod, 2 * quietPeriod);
        }
    }
}
