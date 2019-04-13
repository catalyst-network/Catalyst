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
using Catalyst.Node.Common.Interfaces;
using Dawn;

namespace Catalyst.Node.Common.Helpers.IO
{
    public sealed class SocketClientRegistry<TSocketChannel> : ISocketClientRegistry<TSocketChannel> where TSocketChannel : class, ISocketClient
    {
        public IDictionary<int, ISubscribedSocket<TSocketChannel>> Registry { get; }

        public SocketClientRegistry()
        {
            Registry = new ConcurrentDictionary<int, ISubscribedSocket<TSocketChannel>>();
        }

        /// <summary>
        ///     todo Maybe we put this in the ISocketClient somewhere, as it will always know its endpoint?
        /// </summary>
        /// <param name="socketEndpoint"></param>
        /// <returns></returns>
        public int GenerateClientHashCode(IPEndPoint socketEndpoint)
        {
            Guard.Argument(socketEndpoint, nameof(socketEndpoint)).NotNull();
            return socketEndpoint.GetHashCode();
        }

        /// <inheritdoc />
        public bool AddClientToRegistry(int socketHashCode, ISubscribedSocket<TSocketChannel> subscribedSocket)
        {
            Guard.Argument(subscribedSocket, nameof(subscribedSocket)).NotNull();
            Guard.Argument(socketHashCode, nameof(socketHashCode)).NotZero();
            Guard.Argument(subscribedSocket.SocketChannel.Channel.Active, nameof(subscribedSocket.SocketChannel));
            Guard.Argument(subscribedSocket.Subscription, nameof(subscribedSocket.Subscription)).NotNull();
            return Registry.TryAdd(socketHashCode, subscribedSocket);
        }

        /// <inheritdoc />
        public ISubscribedSocket<TSocketChannel> GetClientFromRegistry(int socketHashCode)
        {
            Guard.Argument(socketHashCode, nameof(socketHashCode)).NotZero();
            var socketChannelAndSubscription = Registry.TryGetValue(socketHashCode, out var socketClient)
                ? socketClient
                : null;
            return socketChannelAndSubscription;
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
