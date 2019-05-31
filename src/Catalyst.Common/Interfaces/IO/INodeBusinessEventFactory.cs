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

using DotNetty.Transport.Channels;

namespace Catalyst.Common.Interfaces.IO
{
    public interface INodeBusinessEventFactory
    {
        /// <summary>Creates a new Rpc Server loop group with a specified amount of threads
        /// located inside <see cref="Microsoft.Extensions.Configuration.IConfigurationRoot"/> </summary>
        /// <returns><see cref="IEventLoopGroup"/>The event loop group</returns>
        IEventLoopGroup NewRpcServerLoopGroup();

        /// <summary>Creates a new UDP Server loop group with a specified amount of threads
        /// located inside <see cref="Microsoft.Extensions.Configuration.IConfigurationRoot"/> </summary>
        /// <returns><see cref="IEventLoopGroup"/>The event loop group</returns>
        IEventLoopGroup NewUdpServerLoopGroup();

        /// <summary>Creates a new UDP Client loop group with a specified amount of threads
        /// located inside <see cref="Microsoft.Extensions.Configuration.IConfigurationRoot"/> </summary>
        /// <returns><see cref="IEventLoopGroup"/>The event loop group</returns>
        IEventLoopGroup NewUdpClientLoopGroup();
    }
}
