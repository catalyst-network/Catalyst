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

namespace Catalyst.Abstractions.IO.EventLoop
{
    public interface IEventLoopGroupFactoryConfiguration
    {
        /// <summary>Gets or sets the TCP server threads.</summary>
        /// <value>The TCP server threads.</value>
        int TcpServerHandlerWorkerThreads { get; set; }

        /// <summary>Gets or sets the TCP client threads.</summary>
        /// <value>The TCP client threads.</value>
        int TcpClientHandlerWorkerThreads { get; set; }

        /// <summary>Gets or sets the UDP server threads.</summary>
        /// <value>The UDP server threads.</value>
        int UdpServerHandlerWorkerThreads { get; set; }

        /// <summary>Gets or sets the UDP client threads.</summary>
        /// <value>The UDP client threads.</value>
        int UdpClientHandlerWorkerThreads { get; set; }
    }
}
