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

namespace Catalyst.Node.Common.Interfaces
{
    /// <summary>
    /// Bunches up a network socket and the Rx subscription used to watch its incoming messages
    /// </summary>
    /// <typeparam name="TSocketChannel">The type of the socket use to transmit information</typeparam>
    public interface ISubscribedSocket<out TSocketChannel>
        where TSocketChannel : class, ISocketClient
    {
        /// <summary>
        /// The socket channel used for out of process communications
        /// </summary>
        TSocketChannel SocketChannel { get; }

        /// <summary>
        /// The (Rx) subscription to the socket channel used to propagate
        /// messages in-process.
        /// </summary>
        IDisposable Subscription { get; }
    }
}
