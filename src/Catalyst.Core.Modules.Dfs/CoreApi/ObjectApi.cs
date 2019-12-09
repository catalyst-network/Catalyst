using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Dfs.UnixFileSystem;
using Lib.P2P;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class ObjectApi : IObjectApi
    {
        internal static readonly IDagNode EmptyNode;
        internal static readonly IDagNode EmptyDirectory;

        private readonly IBlockApi _blockApi;

        static ObjectApi()
        {
            EmptyNode = new DagNode(new byte[0]);
            var _ = EmptyNode.Id;

            var dm = new DataMessage
            {
                Type = DataType.Directory
            };
            using (var pb = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(pb, dm);
                EmptyDirectory = new DagNode(pb.ToArray());
            }

            _ = EmptyDirectory.Id;
        }

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
            var node = new DagNode(block.DataStream);
            return node.Links;
        }

        public Task<IDagNode> NewAsync(string template = null, CancellationToken cancel = default)
        {
            switch (template)
            {
                case null:
                    return Task.FromResult(EmptyNode);
                case "unixfs-dir":
                    return Task.FromResult(EmptyDirectory);
                default:
                    throw new ArgumentException($"Unknown template '{template}'.", "template");
            }
        }

        public Task<IDagNode> NewDirectoryAsync(CancellationToken cancel = default)
        {
            return Task.FromResult(EmptyDirectory);
        }

        public Task<IDagNode> PutAsync(byte[] data,
            IEnumerable<IMerkleLink> links = null,
            CancellationToken cancel = default)
        {
            var node = new DagNode(data, links);
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
            var node = new DagNode(block.DataStream);
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
