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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO
{
    public class IoBase : ISocket, IDisposable
    {
        protected const int BackLogValue = 100;
        protected readonly ILogger Logger;
        protected readonly IEventLoopGroup WorkerEventLoop;

        public IChannel Channel { get; set; }

        protected IoBase(ILogger logger)
        {
            Logger = logger;
            WorkerEventLoop = new MultithreadEventLoopGroup();
        }

        public virtual async Task Shutdown()
        {
            if (Channel != null)
            {
                await Channel.CloseAsync().ConfigureAwait(false);
            }

            if (WorkerEventLoop != null)
            {
                var quietPeriod = TimeSpan.FromMilliseconds(100);
                await WorkerEventLoop
                   .ShutdownGracefullyAsync(quietPeriod, 2 * quietPeriod)
                   .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Logger.Information($"Disposing {GetType().Name}");
                Task.WaitAll(Shutdown());
            }
        }
    }
}
