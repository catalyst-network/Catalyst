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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.IO.Events;
using Dawn;

namespace Catalyst.Common.IO.Transport
{
    public sealed class SocketClientRegistry<TSocketChannel>
        : ISocketClientRegistry<TSocketChannel>
        where TSocketChannel : class, ISocketClient
    {
        public IObservable<IObservableEvent> EventStream { private set;  get; }
        private readonly ReplaySubject<IObservableEvent> _eventReplySubject;
        public IDictionary<int, TSocketChannel> Registry { get; }

        public SocketClientRegistry()
        {
            _eventReplySubject = new ReplaySubject<IObservableEvent>(1);
            EventStream = _eventReplySubject.AsObservable();

            Registry = new ConcurrentDictionary<int, TSocketChannel>();
        }

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
                _eventReplySubject.OnNext(new SocketClientRegistryClientAdded { SocketHashCode = socketHashCode });
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
                _eventReplySubject.OnNext(new SocketClientRegistryClientRemoved { SocketHashCode = socketHashCode });
            }
            return removedFromRegistry;
        }

        public string GetRegistryType()
        {
            return typeof(TSocketChannel).Name;
        }
    }
}
