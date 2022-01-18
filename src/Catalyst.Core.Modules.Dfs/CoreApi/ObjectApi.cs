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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.UnixFs;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class ObjectApi : IObjectApi
    {
        internal static IDagNode GetEmptyNode()
        {
            return new DagNode(new byte[0]);
        }

        internal static IDagNode GetEmptyDirectory()
        {
            var dm = new DataMessage
            {
                Type = DataType.Directory
            };
            using MemoryStream pb = new();
            ProtoBuf.Serializer.Serialize(pb, dm);
            return new DagNode(pb.ToArray());
        }

        private readonly IBlockApi _blockApi;

        public ObjectApi(IBlockApi blockApi)
        {
            _blockApi = blockApi;
        }

        public async Task<Stream> DataAsync(Cid id, CancellationToken cancel = default)
        {
            var node = await GetAsync(id, cancel).ConfigureAwait(false);
            return node.DataStream;
        }

        public async Task<IDagNode> GetAsync(Cid id, CancellationToken cancel = default)
        {
            var block = await _blockApi.GetAsync(id, cancel).ConfigureAwait(false);
            return new DagNode(block.DataStream);
        }

        public async Task<IEnumerable<IMerkleLink>> LinksAsync(Cid id,
            CancellationToken cancel = default)
        {
            if (id.ContentType != "dag-pb")
            {
                return Enumerable.Empty<IMerkleLink>();
            }

            var block = await _blockApi.GetAsync(id, cancel).ConfigureAwait(false);
            DagNode node = new(block.DataStream);
            return node.Links;
        }

        public Task<IDagNode> NewAsync(string template = null, CancellationToken cancel = default)
        {
            switch (template)
            {
                case null:
                    return Task.FromResult(GetEmptyNode());
                case "unixfs-dir":
                    return Task.FromResult(GetEmptyDirectory());
                default:
                    throw new ArgumentException($"Unknown template '{template}'.", nameof(template));
            }
        }

        public Task<IDagNode> NewDirectoryAsync(CancellationToken cancel = default)
        {
            return Task.FromResult(GetEmptyDirectory());
        }

        public Task<IDagNode> PutAsync(byte[] data,
            IEnumerable<IMerkleLink> links = null,
            CancellationToken cancel = default)
        {
            DagNode node = new(data, links);
            return PutAsync(node, cancel);
        }

        public async Task<IDagNode> PutAsync(IDagNode node, CancellationToken cancel = default)
        {
            node.Id = await _blockApi.PutAsync(node.ToArray(), cancel: cancel).ConfigureAwait(false);
            return node;
        }

        public async Task<ObjectStat> StatAsync(Cid id, CancellationToken cancel = default)
        {
            var block = await _blockApi.GetAsync(id, cancel).ConfigureAwait(false);
            DagNode node = new(block.DataStream);
            return new ObjectStat
            {
                BlockSize = block.Size,
                DataSize = node.DataBytes.Length,
                LinkCount = node.Links.Count(),
                LinkSize = block.Size - node.DataBytes.Length,
                CumulativeSize = block.Size + node.Links.Sum(link => link.Size)
            };
        }
    }
}
