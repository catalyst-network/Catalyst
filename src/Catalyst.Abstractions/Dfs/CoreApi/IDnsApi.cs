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

using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   DNS mapping to IPFS.
    /// </summary>
    /// <remarks>
    ///   Multihashes are hard to remember, but domain names are usually easy to
    ///   remember. To create memorable aliases for multihashes, DNS TXT
    ///   records can point to other DNS links, IPFS objects, IPNS keys, etc.
    /// </remarks>
    public interface IDnsApi
    {
        /// <summary>
        ///   Resolve a domain name to an IPFS path.
        /// </summary>
        /// <param name="name">
        ///   An domain name, such as "ipfs.io".
        /// </param>
        /// <param name="recursive">
        ///   Resolve until the result is not a DNS link. Defaults to <b>false</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the resolved IPFS path as a <see cref="string"/>, such as 
        ///   <c>/ipfs/QmYNQJoKGNHTpPxCBPh9KkDpaExgd2duMa3aF6ytMpHdao</c>.
        /// </returns>
        /// <remarks>
        ///   A DNS TXT record with a "dnslink=..." entry is expected to exist.  The
        ///   value of the "dnslink" is an IPFS path to another IPFS object.
        ///   <para>
        ///   A DNS query is generated for both <paramref name="name"/> and
        ///   _dnslink.<paramref name="name"/>.
        ///   </para>
        /// </remarks>
        /// <example>
        ///   <c>ResolveAsync("ipfs.io", recursive: false)</c> produces "/ipns/website.ipfs.io". Whereas,
        ///   <c>ResolveAsync("ipfs.io", recursive: true)</c> produces "/ipfs/QmXZz6vQTMiu6UyGxVgpLB6xJdHvvUbhdWagJQNnxXAjpn".
        /// </example>
        Task<string> ResolveAsync(string name,
            bool recursive = false,
            CancellationToken cancel = default);

        Task<string> ResolveNameAsync(string name,
            bool recursive = false,
            bool nocache = false,
            CancellationToken cancel = default);
    }
}
