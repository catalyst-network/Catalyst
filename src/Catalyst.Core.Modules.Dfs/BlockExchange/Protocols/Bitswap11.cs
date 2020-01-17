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
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.BlockExchange.Protocols;
using Common.Logging;
using Lib.P2P;
using MultiFormats;
using ProtoBuf;
using Semver;
using ProtoBufHelper = Lib.P2P.ProtoBufHelper;

#pragma warning disable 0649 // disable warning about unassinged fields
#pragma warning disable 0169// disable warning about unassinged fields

namespace Catalyst.Core.Modules.Dfs.BlockExchange.Protocols
{
    /// <summary>
    ///   Bitswap Protocol version 1.1.0 
    /// </summary>
    public class Bitswap11 : IBitswapProtocol
    {
        private static ILog log = LogManager.GetLogger(typeof(Bitswap11));

        /// <inheritdoc />
        public string Name { get; } = "ipfs/bitswap";

        /// <inheritdoc />
        public SemVersion Version { get; } = new SemVersion(1, 1);

        /// <inheritdoc />
        public override string ToString() { return $"/{Name}/{Version}"; }

        /// <summary>
        ///   The <see cref="BitswapService"/> service.
        /// </summary>
        public IBitswapService BitswapService { get; set; }

        /// <inheritdoc />
        public async Task ProcessMessageAsync(PeerConnection connection,
            Stream stream,
            CancellationToken cancel = default)
        {
            // There is a race condition between getting the remote identity and
            // the remote sending the first wantlist.
            await connection.IdentityEstablished.Task.ConfigureAwait(false);

            while (true)
            {
                var request = await ProtoBufHelper.ReadMessageAsync<Message>(stream, cancel).ConfigureAwait(false);

                // Process want list
                if (request.wantlist != null && request.wantlist.entries != null)
                {
                    foreach (var entry in request.wantlist.entries)
                    {
                        var cid = Cid.Read(entry.block);
                        if (entry.cancel)
                        {
                            // TODO: Unwant specific to remote peer
                            BitswapService.Unwant(cid);
                        }
                        else
                        {
                            // TODO: Should we have a timeout?
                            var _ = GetBlockAsync(cid, connection.RemotePeer, CancellationToken.None);
                        }
                    }
                }

                // Forward sent blocks to the block service.  Eventually
                // bitswap will here about and them and then continue
                // any tasks (GetBlockAsync) waiting for the block.
                if (request.payload == null)
                {
                    continue;
                }
                
                log.Debug($"got block(s) from {connection.RemotePeer}");
                foreach (var sentBlock in request.payload)
                {
                    await using (var ms = new MemoryStream(sentBlock.prefix))
                    {
                        ms.ReadVarint32();
                        var contentType = ms.ReadMultiCodec().Name;
                        var multiHash = MultiHash.GetHashAlgorithmName(ms.ReadVarint32());
                        await BitswapService.OnBlockReceivedAsync(connection.RemotePeer, sentBlock.data, contentType,
                            multiHash);
                    }
                }
            }
        }

        private async Task GetBlockAsync(Cid cid, Peer remotePeer, CancellationToken cancel)
        {
            // TODO: Determine if we will fetch the block for the remote
            try
            {
                IDataBlock block;
                if (null != await BitswapService.BlockService.StatAsync(cid, cancel).ConfigureAwait(false))
                {
                    block = await BitswapService.BlockService.GetAsync(cid, cancel).ConfigureAwait(false);
                }
                else
                {
                    block = await BitswapService.WantAsync(cid, remotePeer.Id, cancel).ConfigureAwait(false);
                }

                // Send block to remote.
                await using (var stream = await BitswapService.SwarmService.DialAsync(remotePeer, ToString(), cancel).ConfigureAwait(false))
                {
                    await SendAsync(stream, block, cancel).ConfigureAwait(false);
                }

                await BitswapService.OnBlockSentAsync(remotePeer, block).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // eat it
            }
            catch (Exception e)
            {
                log.Warn("getting block for remote failed", e);

                // eat it.
            }
        }

        /// <inheritdoc />
        public async Task SendWantsAsync(Stream stream,
            IEnumerable<WantedBlock> wants,
            bool full = true,
            CancellationToken cancel = default)
        {
            var message = new Message
            {
                wantlist = new Wantlist
                {
                    full = full,
                    entries = wants
                       .Select(w =>
                        {
                            return new Entry
                            {
                                block = w.Id.ToArray()
                            };
                        })
                       .ToArray()
                },
                payload = new List<Block>(0)
            };

            Serializer.SerializeWithLengthPrefix(stream, message, PrefixStyle.Base128);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }

        private async Task SendAsync(Stream stream,
            IDataBlock block,
            CancellationToken cancel = default)
        {
            log.Debug($"Sending block {block.Id}");
            var message = new Message
            {
                payload = new List<Block>
                {
                    new Block
                    {
                        prefix = GetCidPrefix(block.Id),
                        data = block.DataBytes
                    }
                }
            };

            Serializer.SerializeWithLengthPrefix(stream, message, PrefixStyle.Base128);
            await stream.FlushAsync(cancel).ConfigureAwait(false);
        }

        /// <summary>
        ///   Gets the CID "prefix".
        /// </summary>
        /// <param name="id">
        ///   The CID.
        /// </param>
        /// <returns>
        ///   A byte array of consisting of cid version, multicodec and multihash prefix (type + length).
        /// </returns>
        private byte[] GetCidPrefix(Cid id)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteVarint(id.Version);
                ms.WriteMultiCodec(id.ContentType);
                ms.WriteVarint(id.Hash.Algorithm.Code);
                ms.WriteVarint(id.Hash.Digest.Length);
                return ms.ToArray();
            }
        }

        [ProtoContract]
        private sealed class Entry
        {
            [ProtoMember(1)]

            // changed from string to bytes, it makes a difference in JavaScript
            public byte[] block; // the block cid (cidV0 in bitswap 1.0.0, cidV1 in bitswap 1.1.0)

            [ProtoMember(2)]
            public int priority = 1; // the priority (normalized). default to 1

            [ProtoMember(3)]
            public bool cancel; // whether this revokes an entry
        }

        [ProtoContract]
        private sealed class Wantlist
        {
            [ProtoMember(1)]
            public Entry[] entries; // a list of wantlist entries

            [ProtoMember(2)]
            public bool full; // whether this is the full wantlist. default to false
        }

        [ProtoContract]
        private sealed class Block
        {
            [ProtoMember(1)]
            public byte[] prefix; // CID prefix (cid version, multicodec and multihash prefix (type + length)

            [ProtoMember(2)]
            public byte[] data;
        }

        [ProtoContract]
        private sealed class Message
        {
            [ProtoMember(1)]
            public Wantlist wantlist;

            [ProtoMember(2)]
            public byte[][] blocks; // used to send Blocks in bitswap 1.0.0

            [ProtoMember(3)]
            public List<Block> payload; // used to send Blocks in bitswap 1.1.0
        }
    }
}
