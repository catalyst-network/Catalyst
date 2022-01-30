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

using System;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Manages the IPNS (Interplanetary Name Space).
    /// </summary>
    /// <remarks>
    ///   IPNS is a PKI namespace, where names are the hashes of public keys, and
    ///   the private key enables publishing new(signed) values. The default name
    ///   is the node's own <see cref="Peer.Id"/>,
    ///   which is the hash of its public key.
    /// </remarks>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/NAME.md">Name API spec</seealso>
    public interface INameApi
    {
        /// <summary>
        ///   Publish an IPFS name.
        /// </summary>
        /// <param name="path">
        ///   The CID or path to the content to publish.
        /// </param>
        /// <param name="resolve">
        ///   Resolve <paramref name="path"/> before publishing. Defaults to <b>true</b>.
        /// </param>
        /// <param name="key">
        ///   The local key name used to sign the content.  Defaults to "self".
        /// </param>
        /// <param name="lifetime">
        ///   Duration that the record will be valid for.  Defaults to 24 hours.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the <see cref="NamedContent"/> of the published content.
        /// </returns>
        Task<NamedContent> PublishAsync(string path,
            bool resolve = true,
            string key = "self",
            TimeSpan? lifetime = null,
            CancellationToken cancel = default);

        /// <summary>
        ///   Publish an IPFS name.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the content to publish.
        /// </param>
        /// <param name="key">
        ///   The local key name used to sign the content.  Defaults to "self".
        /// </param>
        /// <param name="lifetime">
        ///   Duration that the record will be valid for.  Defaults to 24 hours.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the <see cref="NamedContent"/> of the published content.
        /// </returns>
        Task<NamedContent> PublishAsync(Cid id,
            string key = "self",
            TimeSpan? lifetime = null,
            CancellationToken cancel = default);

        /// <summary>
        ///   Resolve an IPNS name.
        /// </summary>
        /// <param name="name">
        ///   An IPNS address, such as: /ipns/ipfs.io or a CID.
        /// </param>
        /// <param name="recursive">
        ///   Resolve until the result is not an IPNS name. Defaults to <b>false</b>.
        /// </param>
        /// <param name="nocache">
        ///   Do not use cached entries. Defaults to <b>false</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the resolved path as a <see cref="string"/>, such as 
        ///   <c>/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao</c>.
        /// </returns>
        Task<string> ResolveAsync(string name,
            bool recursive = false,
            bool nocache = false,
            CancellationToken cancel = default);
    }
}
