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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Transport
{
    public class SocketBase : ISocket
    {
        protected readonly IChannelFactory ChannelFactory;
        protected readonly ILogger Logger;
        protected readonly IEventLoopGroup WorkerEventLoop;
        protected readonly IEventLoopGroup HandlerWorkerEventLoopGroup;

        public IChannel Channel { get; protected set; }

        protected SocketBase(IChannelFactory channelFactory, ILogger logger, IEventLoopGroup handlerWorkerEventLoopGroup)
        {
            ChannelFactory = channelFactory;
            Logger = logger;
            WorkerEventLoop = new MultithreadEventLoopGroup();
            HandlerWorkerEventLoopGroup = handlerWorkerEventLoopGroup;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            Logger.Debug($"Disposing {0}", GetType().Name);

            var quietPeriod = TimeSpan.FromMilliseconds(100);

            try
            {
                Channel?.Flush();
                var closeChannelTask = Channel?.CloseAsync();
                var closeWorkerLoopTask = WorkerEventLoop?.ShutdownGracefullyAsync(quietPeriod, quietPeriod);
                var handlerWorkerLoopTask =
                    HandlerWorkerEventLoopGroup?.ShutdownGracefullyAsync(quietPeriod, quietPeriod);
                Task.WaitAll(new[] {closeChannelTask, closeWorkerLoopTask, handlerWorkerLoopTask}.Where(t => t != null).ToArray(),
                    quietPeriod * 3);
            }
            catch (Exception e)
            {
                Logger?.Error(e, "Dispose failed to complete.");
            }
        }
    }
}
