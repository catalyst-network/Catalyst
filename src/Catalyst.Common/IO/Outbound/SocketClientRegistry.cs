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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using Catalyst.Common.Interfaces.IO.Outbound;
using Dawn;

namespace Catalyst.Common.IO.Outbound
{
    public sealed class SocketClientRegistry<TSocketChannel>
        : ISocketClientRegistry<TSocketChannel>
        where TSocketChannel : class, ISocketClient
    {
        public IDictionary<int, TSocketChannel> Registry { get; }

        public SocketClientRegistry()
        {
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
            Guard.Argument(socket.Active, nameof(socket))
               .True("Unable to add inactive client to the registry.");
            return Registry.TryAdd(socketHashCode, socket);
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
            return Registry.Remove(socketHashCode);
        }

        public string GetRegistryType()
        {
            return typeof(TSocketChannel).Name;
        }
    }
}
