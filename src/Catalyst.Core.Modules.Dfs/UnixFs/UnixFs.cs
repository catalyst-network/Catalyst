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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.Lib.Dag;
using Lib.P2P;
using ProtoBuf;

namespace Catalyst.Core.Modules.Dfs.UnixFs
{
    /// <summary>
    ///   Support for the *nix file system.
    /// </summary>
    internal class UnixFs
    {
        private static readonly byte[] EmptyData = new byte[0];

        /// <summary>
        ///   Creates a stream that can read the supplied <see cref="Cid"/>.
        /// </summary>
        /// <param name="id">
        ///   The identifier of some content.
        /// </param>
        /// <param name="blockService">
        ///   The source of the cid's data.
        /// </param>
        /// <param name="keyChain">
        ///   Used to decypt the protected data blocks.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   a <see cref="Stream"/> that produces the content of the <paramref name="id"/>.
        /// </returns>
        /// <remarks>
        ///  The id's <see cref="Cid.ContentType"/> is used to determine how to read
        ///  the conent.
        /// </remarks>
        public static Task<Stream> CreateReadStreamAsync(Cid id,
            IBlockApi blockService,
            IKeyApi keyChain,
            CancellationToken cancel)
        {
            switch (id.ContentType)
            {
                // TODO: A content-type registry should be used.
                case "dag-pb":
                    return CreateDagProtoBufStreamAsync(id, blockService, keyChain, cancel);
                case "raw":
                    return CreateRawStreamAsync(id, blockService, cancel);
                case "cms":
                    return CreateCmsStreamAsync(id, blockService, keyChain, cancel);
                default:
                    throw new NotSupportedException($"Cannot read content type '{id.ContentType}'.");
            }
        }

        private static async Task<Stream> CreateRawStreamAsync(Cid id,
            IBlockApi blockService,
            CancellationToken cancel)
        {
            var block = await blockService.GetAsync(id, cancel).ConfigureAwait(false);
            return block.DataStream;
        }

        private static async Task<Stream> CreateDagProtoBufStreamAsync(Cid id,
            IBlockApi blockService,
            IKeyApi keyChain,
            CancellationToken cancel)
        {
            var block = await blockService.GetAsync(id, cancel).ConfigureAwait(false);
            DagNode dag = new(block.DataStream);
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);

            if (dm.Type != DataType.File)
            {
                throw new Exception($"'{id.Encode()}' is not a file.");
            }

            if (dm.Fanout.HasValue)
            {
                throw new NotImplementedException("files with a fanout");
            }

            // Is it a simple node?
            if (dm.BlockSizes == null && !dm.Fanout.HasValue)
            {
                return new MemoryStream(buffer: dm.Data ?? EmptyData, writable: false);
            }

            if (dm.BlockSizes != null)
            {
                return new ChunkedStream(blockService, keyChain, dag);
            }

            throw new Exception($"Cannot determine the file format of '{id}'.");
        }

        private static async Task<Stream> CreateCmsStreamAsync(Cid id,
            IBlockApi blockService,
            IKeyApi keyChain,
            CancellationToken cancel)
        {
            var block = await blockService.GetAsync(id, cancel).ConfigureAwait(false);
            var plain = await keyChain.ReadProtectedDataAsync(block.DataBytes, cancel).ConfigureAwait(false);
            return new MemoryStream(plain, false);
        }
    }
}
