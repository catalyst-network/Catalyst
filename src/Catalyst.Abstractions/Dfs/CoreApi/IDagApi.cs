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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lib.P2P;
using MultiFormats;
using Newtonsoft.Json.Linq;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Manages the IPLD (linked data) Directed Acrylic Graph.
    /// </summary>
    /// <remarks>
    ///   The DAG API is a replacement of the <see cref="IObjectApi"/>, which only supported 'dag-pb'.
    ///   This API supports other IPLD formats, such as cbor, ethereum-block, git, ...
    /// </remarks>
    /// <seealso cref="IObjectApi"/>
    /// <seealso cref="ILinkedNode"/>
    /// <seealso href="https://github.com/ipfs/interface-ipfs-core/blob/master/SPEC/DAG.md">Dag API spec</seealso>
    public interface IDagApi
    {
        /// <summary>
        ///  Put JSON data as an IPLD node.
        /// </summary>
        /// <param name="data">
        ///   The JSON data to send to the network.
        /// </param>
        /// <param name="contentType">
        ///   The content type or format of the <paramref name="data"/>; such as "dag-pb" or "dag-cbor".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-cbor".
        /// </param>
        /// <param name="multiHash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="encoding">
        ///   The <see cref="MultiBase"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the <paramref name="data"/> is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>true</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous put operation. The task's value is
        ///   the data's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(JObject data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default);

        /// <summary>
        ///  Put a stream of JSON as an IPLD node.
        /// </summary>
        /// <param name="data">
        ///   The stream of JSON.
        /// </param>
        /// <param name="contentType">
        ///   The content type or format of the <paramref name="data"/>; such as "dag-pb" or "dag-cbor".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-cbor".
        /// </param>
        /// <param name="multiHash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="encoding">
        ///   The <see cref="MultiBase"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the <paramref name="data"/> is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>true</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous put operation. The task's value is
        ///   the data's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(Stream data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default);

        /// <summary>
        ///  Put an object as an IPLD node.
        /// </summary>
        /// <param name="data">
        ///   The object to add.
        /// </param>
        /// <param name="contentType">
        ///   The content type or format of the <paramref name="data"/>; such as "dag-pb" or "dag-cbor".
        ///   See <see cref="MultiCodec"/> for more details.  Defaults to "dag-cbor".
        /// </param>
        /// <param name="multiHash">
        ///   The <see cref="MultiHash"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="encoding">
        ///   The <see cref="MultiBase"/> algorithm name used to produce the <see cref="Cid"/>.
        /// </param>
        /// <param name="pin">
        ///   If <b>true</b> the <paramref name="data"/> is pinned to local storage and will not be
        ///   garbage collected.  The default is <b>true</b>.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous put operation. The task's value is
        ///   the data's <see cref="Cid"/>.
        /// </returns>
        Task<Cid> PutAsync(Object data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default);

        /// <summary>
        ///   Get an IPLD node.
        /// </summary>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the IPLD node.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   contains the node's content as JSON.
        /// </returns>
        Task<JObject> GetAsync(Cid id, CancellationToken cancel = default);

        /// <summary>
        ///   Gets the content of an IPLD node.
        /// </summary>
        /// <param name="path">
        ///   A path, such as "cid", "/ipfs/cid/" or "cid/a".
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   contains the path's value.
        /// </returns>
        Task<JToken> GetAsync(string path, CancellationToken cancel = default);

        /// <summary>
        ///   Get an IPLD node of the specific type.
        /// </summary>
        /// <typeparam name="T">
        ///   The object's type.
        /// </typeparam>
        /// <param name="id">
        ///   The <see cref="Cid"/> of the IPLD node.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous get operation. The task's value
        ///   is a new instance of the <typeparamref name="T"/> class.
        /// </returns>
        Task<T> GetAsync<T>(Cid id, CancellationToken cancel = default);
    }
}
