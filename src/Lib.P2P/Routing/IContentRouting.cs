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
using System.Threading;
using System.Threading.Tasks;

namespace Lib.P2P.Routing
{
    /// <summary>
    ///    Find information about who has what content.
    /// </summary>
    /// <remarks>
    ///   No IPFS documentation is currently available.  See the 
    ///   <see href="https://godoc.org/github.com/libp2p/go-libp2p-routing">code</see>.
    /// </remarks>
    public interface IContentRouting
    {
        /// <summary>
        ///    Adds the <see cref="Cid"/> to the content routing system.
        /// </summary>
        /// <param name="cid">
        ///   The ID of some content that the peer contains.
        /// </param>
        /// <param name="advertise">
        ///   Advertise the <paramref name="cid"/> to other peers.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        Task ProvideAsync(Cid cid, bool advertise = true, CancellationToken cancel = default);

        /// <summary>
        ///   Find the providers for the specified content.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the content.
        /// </param>
        /// <param name="limit">
        ///   The maximum number of peers to return.  Defaults to 20.
        /// </param>
        /// <param name="providerFound">
        ///   An action to perform when a provider is found.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation that returns
        ///   a sequence of IPFS <see cref="Peer"/>.
        /// </returns>
        Task<IEnumerable<Peer>> FindProvidersAsync(Cid id,
            int limit = 20,
            Action<Peer> providerFound = null,
            CancellationToken cancel = default);
    }
}
