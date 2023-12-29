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

namespace Lib.P2P.Protocols
{
    /// <summary>
    ///   Applies encryption to a <see cref="PeerConnection"/>.
    /// </summary>
    public interface IEncryptionProtocol : IPeerProtocol
    {
        /// <summary>
        ///   Creates an encrypted stream for the connection.
        /// </summary>
        /// <param name="connection">
        ///   A connection between two peers.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is the encrypted stream.
        /// </returns>
        Task<Stream> EncryptAsync(PeerConnection connection, CancellationToken cancel = default);
    }
}
