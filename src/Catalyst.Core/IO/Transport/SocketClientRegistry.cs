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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Abstractions.IO.Events;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Core.IO.Events;
using Dawn;

namespace Catalyst.Core.IO.Transport
{
    public sealed class SocketClientRegistry<TSocketChannel>
        : ISocketClientRegistry<TSocketChannel>, IDisposable
        where TSocketChannel : class, ISocketClient
    {
        private readonly ReplaySubject<ISocketClientRegistryEvent> _eventReplySubject;
        private bool _disposed;

        public SocketClientRegistry(IScheduler scheduler = null)
        {
            var eventScheduler = scheduler ?? Scheduler.Default;

            _eventReplySubject = new ReplaySubject<ISocketClientRegistryEvent>(1, eventScheduler);
            EventStream = _eventReplySubject.AsObservable();

            Registry = new ConcurrentDictionary<int, TSocketChannel>();
        }

        public void Dispose() { Dispose(true); }

        public IObservable<ISocketClientRegistryEvent> EventStream { get; }

        public IDictionary<int, TSocketChannel> Registry { get; }

        /// <inheritdoc />
        public int GenerateClientHashCode(IPEndPoint socketEndpoint)
        {
            Guard.Argument(socketEndpoint, nameof(socketEndpoint)).NotNull();
            return socketEndpoint.GetHashCode();
        }

        /// <inheritdoc />
        public bool AddClientToRegistry(int socketHashCode, TSocketChannel socket)
        {
            Guard.Argument(socket, nameof(socket)).NotNull();
            Guard.Argument(socketHashCode, nameof(socketHashCode)).NotZero();
            Guard.Argument(socket.Channel.Active, nameof(socket))
               .True("Unable to add inactive client to the registry.");

            var addedToRegistry = Registry.TryAdd(socketHashCode, socket);
            if (addedToRegistry)
            {
                _eventReplySubject.OnNext(new SocketClientRegistryClientAdded {SocketHashCode = socketHashCode});
            }

            return addedToRegistry;
        }

        /// <inheritdoc />
        public TSocketChannel GetClientFromRegistry(int socketHashCode)
        {
            Guard.Argument(socketHashCode, nameof(socketHashCode)).NotZero();
            var socketChannel = Registry.TryGetValue(socketHashCode, out var socketClient)
                ? socketClient
                : null;
            return socketChannel;
        }

        /// <inheritdoc />
        public bool RemoveClientFromRegistry(int socketHashCode)
        {
            Guard.Argument(socketHashCode, nameof(socketHashCode)).NotZero();
            var removedFromRegistry = Registry.Remove(socketHashCode);
            if (removedFromRegistry)
            {
                _eventReplySubject.OnNext(new SocketClientRegistryClientRemoved {SocketHashCode = socketHashCode});
            }

            return removedFromRegistry;
        }

        public string GetRegistryType() { return typeof(TSocketChannel).Name; }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _eventReplySubject.Dispose();
            }

            _disposed = true;
        }
    }
}
