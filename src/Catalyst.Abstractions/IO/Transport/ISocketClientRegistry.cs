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
using System.Net;
using Catalyst.Abstractions.IO.Events;

namespace Catalyst.Abstractions.IO.Transport
{
    public interface ISocketClientRegistry<TSocketChannel> where TSocketChannel : class, ISocketClient
    {
        IObservable<ISocketClientRegistryEvent> EventStream { get; }

        IDictionary<int, TSocketChannel> Registry { get; }

        /// <summary>
        ///     Generates a hashcode from the socket endpoint so we can find it in dict.
        /// </summary>
        /// <param name="socketEndpoint"></param>
        /// <returns></returns>
        int GenerateClientHashCode(IPEndPoint socketEndpoint);

        /// <summary>
        ///     Adds an active ISocketClient to the registry.
        /// </summary>
        /// <param name="socketHashCode"></param>
        /// <param name="socket"></param>
        /// <returns></returns>
        bool AddClientToRegistry(int socketHashCode, TSocketChannel socket);

        /// <summary>
        ///     Tries to get a socket client from registry.
        /// </summary>
        /// <param name="socketHashCode"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        TSocketChannel GetClientFromRegistry(int socketHashCode);

        /// <summary>
        ///     Removes a ISocketClient from registry.
        /// </summary>
        /// <param name="socketHashCode"></param>
        /// <returns></returns>
        bool RemoveClientFromRegistry(int socketHashCode);

        string GetRegistryType();
    }
}
