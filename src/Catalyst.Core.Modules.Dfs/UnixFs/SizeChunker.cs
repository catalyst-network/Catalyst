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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Dag;
using ProtoBuf;

namespace Catalyst.Core.Modules.Dfs.UnixFileSystem
{
    /// <summary>
    ///     Chunks a data stream into data blocks based upon a size.
    /// </summary>
    public class SizeChunker
    {
        /// <summary>
        ///     Performs the chunking.
        /// </summary>
        /// <param name="stream">
        ///     The data source.
        /// </param>
        /// <param name="name">
        ///     A name for the data.
        /// </param>
        /// <param name="options">
        ///     The options when adding data to the IPFS file system.
        /// </param>
        /// <param name="blockService">
        ///     The destination for the chunked data block(s).
        /// </param>
        /// <param name="keyChain">
        ///     Used to protect the chunked data blocks(s).
        /// </param>
        /// <param name="cancel">
        ///     Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException" /> is raised.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation. The task's value is
        ///     the sequence of file system nodes of the added data blocks.
        /// </returns>
        public async Task<List<UnixFsNode>> ChunkAsync(Stream stream,
            string name,
            AddFileOptions options,
            IBlockApi blockService,
            IKeyApi keyChain,
            CancellationToken cancel)
        {
            var protecting = !string.IsNullOrWhiteSpace(options.ProtectionKey);
            var nodes = new List<UnixFsNode>();
            var chunkSize = options.ChunkSize;
            var chunk = new byte[chunkSize];
            var chunking = true;
            var totalBytes = 0UL;

            while (chunking)
            {
                // Get an entire chunk.
                var length = 0;
                while (length < chunkSize)
                {
                    var n = await stream.ReadAsync(chunk, length, chunkSize - length, cancel).ConfigureAwait(false);
                    if (n < 1)
                    {
                        chunking = false;
                        break;
                    }

                    length += n;
                    totalBytes += (uint) n;
                }

                //  Only generate empty block, when the stream is empty.
                if (length == 0 && nodes.Count > 0)
                {
                    chunking = false;
                    break;
                }

                if (options.Progress != null)
                {
                    options.Progress.Report(new TransferProgress
                    {
                        Name = name,
                        Bytes = totalBytes
                    });
                }

                // if protected data, then get CMS structure.
                if (protecting)
                {
                    // TODO: Inefficent to copy chunk, use ArraySegment in DataMessage.Data
                    var plain = new byte[length];
                    Array.Copy(chunk, plain, length);
                    var cipher = await keyChain.CreateProtectedDataAsync(options.ProtectionKey, plain, cancel)
                       .ConfigureAwait(false);
                    var cid = await blockService.PutAsync(
                        cipher,
                        "cms",
                        options.Hash,
                        options.Encoding,
                        options.Pin,
                        cancel).ConfigureAwait(false);
                    nodes.Add(new UnixFsNode
                    {
                        Id = cid,
                        Size = length,
                        DagSize = cipher.Length,
                        Links = UnixFsLink.None
                    });
                }
                else if (options.RawLeaves)
                {
                    // TODO: Inefficent to copy chunk, use ArraySegment in DataMessage.Data
                    var data = new byte[length];
                    Array.Copy(chunk, data, length);
                    var cid = await blockService.PutAsync(
                        data,
                        "raw",
                        options.Hash,
                        options.Encoding,
                        options.Pin,
                        cancel).ConfigureAwait(false);
                    nodes.Add(new UnixFsNode
                    {
                        Id = cid,
                        Size = length,
                        DagSize = length,
                        Links = UnixFsLink.None
                    });
                }
                else
                {
                    // Build the DAG.
                    var dm = new DataMessage
                    {
                        Type = DataType.File,
                        FileSize = (ulong) length
                    };
                    if (length > 0)
                    {
                        // TODO: Inefficent to copy chunk, use ArraySegment in DataMessage.Data
                        var data = new byte[length];
                        Array.Copy(chunk, data, length);
                        dm.Data = data;
                    }

                    var pb = new MemoryStream();
                    Serializer.Serialize(pb, dm);
                    var dag = new DagNode(pb.ToArray(), null, options.Hash);

                    // Save it.
                    dag.Id = await blockService.PutAsync(
                        dag.ToArray(),
                        multiHash: options.Hash,
                        encoding: options.Encoding,
                        pin: options.Pin,
                        cancel: cancel).ConfigureAwait(false);

                    var node = new UnixFsNode
                    {
                        Id = dag.Id,
                        Size = length,
                        DagSize = dag.Size,
                        Links = UnixFsLink.None
                    };
                    nodes.Add(node);
                }
            }

            return nodes;
        }
    }
}
