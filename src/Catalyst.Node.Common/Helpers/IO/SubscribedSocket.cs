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
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.IO
{
    /// <inheritdoc />
    public sealed class SubscribedSocket<TSocketChannel>
        : ISubscribedSocket<TSocketChannel> where TSocketChannel : class, ISocketClient
    {
        public SubscribedSocket(IDisposable subscription, TSocketChannel socketChannel)
        {
            Subscription = subscription;
            SocketChannel = socketChannel;
        }

        /// <inheritdoc />
        public IDisposable Subscription { get; }

        /// <inheritdoc />
        public TSocketChannel SocketChannel { get; }
    }
}
