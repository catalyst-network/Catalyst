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
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.Lib.Dag;
using Lib.P2P;
using ProtoBuf;

namespace Catalyst.Core.Modules.Dfs.UnixFs
{
    /// <summary>
    ///   Provides read-only access to a chunked file.
    /// </summary>
    /// <remarks>
    ///   Internal class to support <see cref="UnixFs"/>.
    /// </remarks>
    public sealed class ChunkedStream : Stream
    {
        private sealed class BlockInfo
        {
            public Cid Id;
            public long Position;
        }

        private List<BlockInfo> blocks = new List<BlockInfo>();
        private long fileSize;

        /// <summary>
        ///   Creates a new instance of the <see cref="ChunkedStream"/> class with
        ///   the specified <see cref="IBlockApi"/> and <see cref="DagNode"/>.
        /// </summary>
        /// <param name="blockService"></param>
        /// <param name="keyChain"></param>
        /// <param name="dag"></param>
        internal ChunkedStream(IBlockApi blockService, IKeyApi keyChain, IDagNode dag)
        {
            BlockService = blockService;
            KeyChain = keyChain;
            var links = dag.Links.ToArray();
            var dm = Serializer.Deserialize<DataMessage>(dag.DataStream);
            if (dm.FileSize != null) fileSize = (long) dm.FileSize;
            ulong position = 0;
            for (var i = 0; i < dm.BlockSizes.Length; ++i)
            {
                blocks.Add(new BlockInfo
                {
                    Id = links[i].Id,
                    Position = (long) position
                });
                position += dm.BlockSizes[i];
            }
        }

        private IBlockApi BlockService { get; }
        private IKeyApi KeyChain { get; }

        /// <inheritdoc />
        public override long Length => fileSize;

        /// <inheritdoc />
        public override void SetLength(long value) { throw new NotSupportedException(); }

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override void Flush() { }

        /// <inheritdoc />
        public override long Position { get; set; }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            return Position;
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancel)
        {
            var block = await GetBlockAsync(Position, cancel).ConfigureAwait(false);
            var k = Math.Min(count, block.Count);
            if (k <= 0)
            {
                return k;
            }
            
            Array.Copy(block.Array ?? throw new NullReferenceException(), block.Offset, buffer, offset, k);
            Position += k;

            return k;
        }

        private BlockInfo _currentBlock;
        private byte[] _currentData;

        private async Task<ArraySegment<byte>> GetBlockAsync(long position, CancellationToken cancel)
        {
            if (position >= Length)
            {
                return new ArraySegment<byte>();
            }

            var need = blocks.Last(b => b.Position <= position);
            if (need != _currentBlock)
            {
                var stream = await UnixFs.CreateReadStreamAsync(need.Id, BlockService, KeyChain, cancel)
                   .ConfigureAwait(false);
                _currentBlock = need;
                _currentData = new byte[stream.Length];
                for (int i = 0, n; i < stream.Length; i += n)
                {
                    n = await stream.ReadAsync(_currentData, i, (int) stream.Length - i, cancel);
                }
            }

            var offset = (int) (position - _currentBlock.Position);
            return new ArraySegment<byte>(_currentData, offset, _currentData.Length - offset);
        }
    }
}
