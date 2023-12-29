#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P
{
    /// <summary>
    ///   Provides access to a private network of peers.
    /// </summary>
    /// <remarks>
    ///   The <see cref="SwarmService"/> calls the network protector whenever a connection
    ///   is being established with another peer.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
    public interface INetworkProtector
    {
        /// <summary>
        ///   Creates a protected stream for the connection.
        /// </summary>
        /// <param name="connection">
        ///   A connection between two peers.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the protected stream.
        /// </returns>
        /// <remarks>
        ///   <b>ProtectAsync</b> is called after the transport level has established
        ///   the connection.
        ///   <para>
        ///   An exception is thrown if the remote peer is not a member of
        ///   the private network.
        ///   </para>
        /// </remarks>
        Task<Stream> ProtectAsync(PeerConnection connection, CancellationToken cancel = default);
    }
}
