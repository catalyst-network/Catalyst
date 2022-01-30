#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MultiFormats;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Manages the list of initial peers.
    /// </summary>
    /// <remarks>
    ///  The API manipulates the "bootstrap list", which contains
    ///  the addresses of the bootstrap nodes. These are the trusted peers from
    ///  which to learn about other peers in the network.
    /// </remarks>
    /// <seealso cref="MultiAddress"/>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/BOOTSTRAP.md">Bootstrap API spec</seealso>
    public interface IBootstrapApi
    {
        /// <summary>
        ///   Adds a new peer.
        /// </summary>
        /// <param name="address">
        ///   The address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the address that was added or <b>null</b> if the address is already
        ///   in the bootstrap list.
        /// </returns>
        Task<MultiAddress> AddAsync(MultiAddress address, CancellationToken cancel = default);

        /// <summary>
        ///   Adds the default peers to the list.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the sequence of addresses that were added.
        /// </returns>
        Task<IEnumerable<MultiAddress>> AddDefaultsAsync(CancellationToken cancel = default);

        /// <summary>
        ///   List all the peers.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   a sequence of addresses.
        /// </returns>
        Task<IEnumerable<MultiAddress>> ListAsync(CancellationToken cancel = default);

        /// <summary>
        ///   Delete the specified peer.
        /// </summary>
        /// <param name="address">
        ///   The address must end with the ipfs protocol and the public ID
        ///   of the peer.  For example "/ip4/104.131.131.82/tcp/4001/ipfs/QmaCpDMGvV2BGHeYERUEnRQAwe3N8SzbUtfsmvsqQLuvuJ"
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the address that was removed or <b>null</b> if the <paramref name="address"/>
        ///   is not in the bootstrap list.
        /// </returns>
        Task<MultiAddress> RemoveAsync(MultiAddress address, CancellationToken cancel = default);

        /// <summary>
        ///   Remove all the peers.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. 
        /// </returns>
        Task RemoveAllAsync(CancellationToken cancel = default);
    }
}
