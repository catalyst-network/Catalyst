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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P.Cryptography;

namespace Lib.P2P.SecureCommunication
{
    /// <summary>
    ///   Provides access to a private network of peers that
    ///   uses a <see cref="PreSharedKey"/>.
    /// </summary>
    /// <remarks>
    ///   The <see cref="SwarmService"/> calls the network protector whenever a connection
    ///   is being established with another peer.
    /// </remarks>
    /// <seealso href="https://github.com/libp2p/specs/blob/master/pnet/Private-Networks-PSK-V1.md"/>
    public class Psk1Protector : INetworkProtector
    {
        /// <summary>
        ///   The key of the private network.
        /// </summary>
        /// <value>
        ///   Only peers with this key can be communicated with.
        /// </value>
        public PreSharedKey Key { private get; set; }

        /// <inheritdoc />
        public Task<Stream> ProtectAsync(PeerConnection connection,
            CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<Stream>(new Psk1Stream(connection.Stream, Key));
        }
    }
}
