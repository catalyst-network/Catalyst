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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Lib.IO;
using Catalyst.Core.Modules.Dfs.UnixFs;
using ICSharpCode.SharpZipLib.Tar;
using Lib.P2P;
using ProtoBuf;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed partial class UnixFsApi : IUnixFsApi
    {
        private readonly IDhtApi _dhtApi;
        private readonly IBlockApi _blockApi;
        private readonly IKeyApi _keyApi;
        private readonly INameApi _nameApi;
        private readonly DfsState _dfsState;

        /// <summary>
        /// @TODO magic numbers be the devils work, WHY ONLY 174??
        /// </summary>
        private const int DefaultLinksPerBlock = 174;

        public UnixFsApi(IDhtApi dhtApi, IBlockApi blockApi, IKeyApi keyApi, INameApi nameApi, DfsState dfsState)
        {
            _dhtApi = dhtApi;
            _blockApi = blockApi;
            _keyApi = keyApi;
            _nameApi = nameApi;
            _dfsState = dfsState;
        }

        public async Task<IFileSystemNode> AddFileAsync(string path,
            AddFileOptions options = default,
            CancellationToken cancel = default)
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await AddAsync(stream, Path.GetFileName(path), options, cancel).ConfigureAwait(false);
        }

        public async Task<IFileSystemNode> AddTextAsync(string text,
            AddFileOptions options = default,
            CancellationToken cancel = default)
        {
            await using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
            return await AddAsync(ms, "", options, cancel).ConfigureAwait(false);
        }

        public async Task<IFileSystemNode> AddAsync(Stream stream,
            string name,
            AddFileOptions options,
            CancellationToken cancel)
        {
            options ??= new AddFileOptions();

            // TODO: various options
            if (options.Trickle)
            {
                throw new NotImplementedException("Trickle");
            }

            var blockService = GetBlockService(options);

            var chunker = new SizeChunker();
            var nodes = await chunker.ChunkAsync(stream, name, options, blockService, _keyApi, cancel)
               .ConfigureAwait(false);

            // Multiple nodes for the file?
            var node = await BuildTreeAsync(nodes, options, cancel);

            // Wrap in directory?
            if (options.Wrap)
            {
                var link = node.ToLink(name);
                var wlinks = new[]
                {
                    link
                };
                node = await CreateDirectoryAsync(wlinks, options, cancel).ConfigureAwait(false);
            }
            else
            {
                node.Name = name;
            }

            // Advertise the root node.
            if (options.Pin && _dfsState.IsStarted)
            {
                await _dhtApi.ProvideAsync(node.Id, advertise: true, cancel: cancel).ConfigureAwait(false);
            }

            // Return the file system node.
            return node;
        }

        private async Task<UnixFsNode> BuildTreeAsync(IEnumerable<UnixFsNode> nodes,
            AddFileOptions options,
            CancellationToken cancel)
        {
            while (true)
            {
                var fsNodes = nodes as UnixFsNode[] ?? nodes.ToArray();
                if (fsNodes.Length == 1)
                {
                    return fsNodes.First();
                }

                // Bundle DefaultLinksPerBlock links into a block.
                var tree = new List<UnixFsNode>();
                for (var i = 0;; ++i)
                {
                    var bundle = fsNodes.Skip(DefaultLinksPerBlock * i).Take(DefaultLinksPerBlock);
                    var unixFsNodes = bundle.ToList();
                    if (!unixFsNodes.Any())
                    {
                        break;
                    }

                    var node = await BuildTreeNodeAsync(unixFsNodes, options, cancel).ConfigureAwait(false);
                    tree.Add(node);
                }

                nodes = tree;
            }
        }

        private async Task<UnixFsNode> BuildTreeNodeAsync(IEnumerable<UnixFsNode> nodes,
            AddFileOptions options,
            CancellationToken cancel)
        {
            var blockService = GetBlockService(options);

            // Build the DAG that contains all the file nodes.
            var unixFsNodes = nodes as UnixFsNode[] ?? nodes.ToArray();
            var links = unixFsNodes.Select(n => n.ToLink()).ToArray();
            var fileSize = (ulong) unixFsNodes.Sum(n => n.Size);
            var dagSize = unixFsNodes.Sum(n => n.DagSize);
            var dm = new DataMessage
            {
                Type = DataType.File,
                FileSize = fileSize,
                BlockSizes = unixFsNodes.Select(n => (ulong) n.Size).ToArray()
            };
            var pb = new MemoryStream();
            Serializer.Serialize(pb, dm);
            var dag = new DagNode(pb.ToArray(), links);

            // Save it.
            dag.Id = await blockService.PutAsync(
                data: dag.ToArray(),
                encoding: options.Encoding,
                pin: options.Pin,
                cancel: cancel).ConfigureAwait(false);

            return new UnixFsNode
            {
                Id = dag.Id,
                Size = (long) dm.FileSize,
                DagSize = dagSize + dag.Size,
                Links = links
            };
        }

        public async Task<IFileSystemNode> AddDirectoryAsync(string path,
            bool recursive = true,
            AddFileOptions options = default,
            CancellationToken cancel = default)
        {
            options ??= new AddFileOptions();
            options.Wrap = false;

            // Add the files and sub-directories.
            path = Path.GetFullPath(path);
            var files = Directory.EnumerateFiles(path).OrderBy(s => s).Select(p => AddFileAsync(p, options, cancel));
            if (recursive)
            {
                var folders = Directory.EnumerateDirectories(path).OrderBy(s => s)
                   .Select(dir => AddDirectoryAsync(dir, true, options, cancel));
                files = files.Union(folders);
            }

            var nodes = await Task.WhenAll(files).ConfigureAwait(false);

            // Create the DAG with links to the created files and sub-directories
            var links = nodes.Select(node => node.ToLink()).ToArray();
            var fsn = await CreateDirectoryAsync(links, options, cancel).ConfigureAwait(false);
            fsn.Name = Path.GetFileName(path);
            return fsn;
        }

        private async Task<UnixFsNode> CreateDirectoryAsync(IEnumerable<IFileSystemLink> links,
            AddFileOptions options,
            CancellationToken cancel)
        {
            var dm = new DataMessage
            {
                Type = DataType.Directory
            };
            var pb = new MemoryStream();
            Serializer.Serialize(pb, dm);
            var fileSystemLinks = links as IFileSystemLink[] ?? links.ToArray();
            var dag = new DagNode(pb.ToArray(), fileSystemLinks);

            // Save it.
            var cid = await GetBlockService(options).PutAsync(
                data: dag.ToArray(),
                multiHash: options.Hash,
                encoding: options.Encoding,
                pin: options.Pin,
                cancel: cancel).ConfigureAwait(false);

            return new UnixFsNode
            {
                Id = cid,
                Links = fileSystemLinks,
                IsDirectory = true
            };
        }

        public async Task<IFileSystemNode> ListFileAsync(string path,
            CancellationToken cancel = default)
        {
            var r = await _nameApi.ResolveAsync(path, true, false, cancel).ConfigureAwait(false);
            var cid = Cid.Decode(r.Remove(0, 6));

            var block = await _blockApi.GetAsync(cid, cancel).ConfigureAwait(false);

            switch (cid.ContentType)
            {
                // TODO: A content-type registry should be used.
                case "dag-pb":
                    // fall thru
                    break;
                case "raw":
                    return new UnixFsNode
                    {
                        Id = cid,
                        Size = block.Size
                    };
                case "cms":
                    return new UnixFsNode
                    {
                        Id = cid,
                        Size = block.Size
                    };
                default:
                    throw new NotSupportedException($"Cannot read content type '{cid.ContentType}'.");
            }

            var dag = new DagNode(block.DataStream);
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
            var fsn = new UnixFsNode
            {
                Id = cid,
                Links = dag.Links.Select(l => new UnixFsLink
                {
                    Id = l.Id,
                    Name = l.Name,
                    Size = l.Size
                }).ToArray(),
                IsDirectory = dm.Type == DataType.Directory,
                Size = (long) (dm.FileSize ?? 0)
            };

            return fsn;
        }

        public async Task<string> ReadAllTextAsync(string path, CancellationToken cancel = default)
        {
            await using (var data = await ReadFileAsync(path, cancel).ConfigureAwait(false))
            {
                using (var text = new StreamReader(data))
                {
                    return await text.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        public async Task<Stream> ReadFileAsync(string path, CancellationToken cancel = default)
        {
            var r = await _nameApi.ResolveAsync(path, true, false, cancel).ConfigureAwait(false);
            var cid = Cid.Decode(r.Remove(0, 6));
            return await UnixFs.UnixFs.CreateReadStreamAsync(cid, _blockApi, _keyApi, cancel).ConfigureAwait(false);
        }

        public async Task<Stream> ReadFileAsync(string path,
            long offset,
            long count = 0,
            CancellationToken cancel = default)
        {
            var stream = await ReadFileAsync(path, cancel).ConfigureAwait(false);
            return new SlicedStream(stream, offset, count);
        }

        public async Task<Stream> GetAsync(string path,
            bool compress = false,
            CancellationToken cancel = default)
        {
            var r = await _nameApi.ResolveAsync(path, true, false, cancel).ConfigureAwait(false);
            var cid = Cid.Decode(r.Remove(0, 6));
            var ms = new MemoryStream();
            await using (var tarStream = new TarOutputStream(ms, 1))
            {
                using (var archive = TarArchive.CreateOutputTarArchive(tarStream))
                {
                    archive.IsStreamOwner = false;
                    await AddTarNodeAsync(cid, cid.Encode(), tarStream, cancel).ConfigureAwait(false);
                }
            }

            ms.Position = 0;
            return ms;
        }

        private async Task AddTarNodeAsync(Cid cid, string name, TarOutputStream tar, CancellationToken cancel)
        {
            var block = await _blockApi.GetAsync(cid, cancel).ConfigureAwait(false);
            var dm = new DataMessage
            {
                Type = DataType.Raw
            };
            DagNode dag = null;

            if (cid.ContentType == "dag-pb")
            {
                dag = new DagNode(block.DataStream);
                dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
            }

            var entry = new TarEntry(new TarHeader());
            var header = entry.TarHeader;
            header.Mode = 0x1ff; // 777 in octal
            header.LinkName = string.Empty;
            header.UserName = string.Empty;
            header.GroupName = string.Empty;
            header.Version = "00";
            header.Name = name;
            header.DevMajor = 0;
            header.DevMinor = 0;
            header.UserId = 0;
            header.GroupId = 0;
            header.ModTime = DateTime.Now;

            if (dm.Type == DataType.Directory)
            {
                header.TypeFlag = TarHeader.LF_DIR;
                header.Size = 0;
                tar.PutNextEntry(entry);
                tar.CloseEntry();

                // Recurse over files and subdirectories
                foreach (var link in dag?.Links)
                {
                    await AddTarNodeAsync(link.Id, $"{name}/{link.Name}", tar, cancel).ConfigureAwait(false);
                }
            }
            else // Must be a file
            {
                var content = await ReadFileAsync(cid, cancel).ConfigureAwait(false);
                header.TypeFlag = TarHeader.LF_NORMAL;
                header.Size = content.Length;
                tar.PutNextEntry(entry);
                await content.CopyToAsync(tar, cancel);
                tar.CloseEntry();
            }
        }

        private IBlockApi GetBlockService(AddFileOptions options)
        {
            return options.OnlyHash
                ? new HashOnlyBlockService()
                : _blockApi;
        }
    }
}
