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
using Catalyst.Common.Interfaces.IO.Transport;
using Dawn;

namespace Catalyst.Common.IO.Transport
{
    public interface iEventType
    {

    }

    public interface iObservableEvent
    {

    }

    public class ObservableEvent<T> where T : iObservableEvent
    {

    }

    public class SocketClientRegistryEvent : iObservableEvent
    {

    }

    public class SocketClientRegistryClientAdded : SocketClientRegistryEvent
    {
        public int SocketHashCode { set; get; }
    }

    public class SocketClientRegistryClientRemoved : SocketClientRegistryEvent
    {
        public int SocketHashCode { set; get; }
    }

    public sealed class SocketClientRegistry<TSocketChannel>
        : ISocketClientRegistry<TSocketChannel>
        where TSocketChannel : class, ISocketClient
    {
        public IObservable<SocketClientRegistryEvent> EventStream { get; }
        private ReplaySubject<SocketClientRegistryEvent> EventReplySubject = new ReplaySubject<SocketClientRegistryEvent>(1);
        public IDictionary<int, TSocketChannel> Registry { get; }

        public SocketClientRegistry()
        {
            EventStream = EventReplySubject.AsObservable();

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
                EventReplySubject.OnNext(new SocketClientRegistryClientAdded { SocketHashCode = socketHashCode });
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
                EventReplySubject.OnNext(new SocketClientRegistryClientRemoved { SocketHashCode = socketHashCode });
            }
            return removedFromRegistry;
        }

        public string GetRegistryType()
        {
            return typeof(TSocketChannel).Name;
        }
    }
}
