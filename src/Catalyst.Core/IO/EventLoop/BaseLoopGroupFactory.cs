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
using System.Threading;
using System.Threading.Tasks;
using Dawn;
using DotNetty.Transport.Channels;

namespace Catalyst.Core.IO.EventLoop
{
    /// <summary>
    /// The <see cref="BaseLoopGroupFactory"/> class keeps references of event loop groups that are created
    /// and holds them from disposing
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class BaseLoopGroupFactory : IDisposable
    {
        /// <summary>The quiet period for the event loop group before shutdown</summary>
        private readonly long QuietPeriod = 100;

        private int _disposeCounter;

        /// <summary>The event loop group list</summary>
        private readonly List<IEventLoopGroup> _eventLoopGroupList;

        /// <summary>The handler worker event loop group</summary>
        internal IEventLoopGroup HandlerWorkerEventLoopGroup;

        /// <summary>The socket io event loop group</summary>
        internal IEventLoopGroup SocketIoEventLoopGroup;

        /// <summary>Initializes a new instance of the <see cref="BaseLoopGroupFactory"/> class.</summary>
        public BaseLoopGroupFactory()
        {
            _eventLoopGroupList = new List<IEventLoopGroup>();
        }

        /// <summary>Creates new Event Loop Group.</summary>
        /// <param name="nEventLoop">The number of event loops.</param>
        /// <returns></returns>
        internal IEventLoopGroup NewEventLoopGroup(int nEventLoop)
        {
            Guard.Argument(nEventLoop).Positive();
            var eventLoopGroup = new MultithreadEventLoopGroup(nEventLoop);
            _eventLoopGroupList.Add(eventLoopGroup);
            return eventLoopGroup;
        }

        /// <summary>Creates new event loop group with the default amount of event loops <see cref="MultithreadEventLoopGroup"/>.</summary>
        /// <returns></returns>
        internal IEventLoopGroup NewEventLoopGroup()
        {
            var eventLoopGroup = new MultithreadEventLoopGroup();
            _eventLoopGroupList.Add(eventLoopGroup);
            return eventLoopGroup;
        }
        
        public IEventLoopGroup GetOrCreateSocketIoEventLoopGroup()
        {
            return SocketIoEventLoopGroup ?? (SocketIoEventLoopGroup = NewEventLoopGroup());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || Interlocked.Increment(ref _disposeCounter) > 1)
            {
                return;
            }
            
            Task[] disposeTasks = _eventLoopGroupList.Select(t =>
                    t.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(QuietPeriod), TimeSpan.FromMilliseconds(QuietPeriod * 3)))
               .ToArray();

            Task.WaitAll(disposeTasks, TimeSpan.FromMilliseconds(QuietPeriod * 4 * disposeTasks.Length));
            _eventLoopGroupList.Clear();
            HandlerWorkerEventLoopGroup = null;
            SocketIoEventLoopGroup = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
