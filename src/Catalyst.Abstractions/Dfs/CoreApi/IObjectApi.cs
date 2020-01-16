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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Manages the IPFS Directed Acrylic Graph.
    /// </summary>
    /// <remarks>
    ///   <note>
    ///   This is being obsoleted by <see cref="IDagApi"/>.
    ///   </note>
    /// </remarks>
    /// <seealso cref="IDagApi"/>
    /// <seealso cref="Catalyst.Ipfs.Core.DagNode"/>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/OBJECT.md">Object API spec</seealso>
    public interface IObjectApi
    {
        /// <summary>
        ///   Creates a new file directory in IPFS.
        /// </summary>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a <see cref="Catalyst.Ipfs.Core.DagNode"/> to the new directory.
        /// </returns>
        /// <remarks>
        ///   Equivalent to <c>NewAsync("unixfs-dir")</c>.
        /// </remarks>
        Task<IDagNode> NewDirectoryAsync(CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Create a new MerkleDAG node, using a specific layout.
        /// </summary>
        /// <param name="template"><b>null</b> or "unixfs-dir".</param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a <see cref="Catalyst.Ipfs.Core.DagNode"/> to the new directory.
        /// </returns>
        /// <remarks>
        ///  Caveat: So far, only UnixFS object layouts are supported.
        /// </remarks>
        Task<IDagNode> NewAsync(string template = null, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Fetch a MerkleDAG node.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> to the node.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a <see cref="Catalyst.Ipfs.Core.DagNode"/>.
        /// </returns>
        Task<IDagNode> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Information on a MerkleDag node.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the node.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///    A task that represents the asynchronous operation. The task's value
        ///    contains the <see cref="ObjectStat"/>.
        /// </returns>
        Task<ObjectStat> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Store a MerkleDAG node.
        /// </summary>
        /// <param name="data">
        ///   The opaque data, can be <b>null</b>.
        /// </param>
        /// <param name="links">
        ///   The links to other nodes.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a <see cref="Catalyst.Ipfs.Core.DagNode"/>.
        /// </returns>
        Task<IDagNode> PutAsync(byte[] data,
            IEnumerable<IMerkleLink> links = null,
            CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Store a MerkleDAG node.
        /// </summary>
        /// <param name="node">A merkle dag</param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a <see cref="Catalyst.Ipfs.Core.DagNode"/>.
        /// </returns>
        Task<IDagNode> PutAsync(IDagNode node, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Get the data of a MerkleDAG node.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the node.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a stream of data.
        /// </returns>
        /// <remarks>
        ///   The caller must dispose the returned <see cref="Stream"/>.
        /// </remarks>
        Task<Stream> DataAsync(Cid id, CancellationToken cancel = default(CancellationToken));

        /// <summary>
        ///   Get the links of a MerkleDAG node.
        /// </summary>
        /// <param name="id">
        ///   The unique id of the node.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value
        ///   is a sequence of links to the immediate children.
        /// </returns>
        /// <remarks>
        ///   <b>LinksAsync</b> returns the immediate child links of the <paramref name="id"/>.
        ///   To get all the children, this code can be used.
        ///   <code>
        ///   async Task&lt;List&lt;IMerkleLink>> AllLinksAsync(Cid cid)
        ///   {
        ///     var i = 0;
        ///     var allLinks = new List&lt;IMerkleLink>();
        ///     while (cid != null)
        ///     {
        ///        var links = await ipfs.Object.LinksAsync(cid);
        ///        allLinks.AddRange(links);
        ///        cid = (i &lt; allLinks.Count) ? allLinks[i++].Id : null;
        ///     }
        ///     return allLinks;
        ///   }
        ///   </code>
        /// </remarks>
        Task<IEnumerable<IMerkleLink>> LinksAsync(Cid id, CancellationToken cancel = default(CancellationToken));
    }
}
