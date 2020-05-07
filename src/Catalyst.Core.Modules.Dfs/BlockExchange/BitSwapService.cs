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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.BlockExchange.Protocols;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.BlockExchange.Protocols;
using Common.Logging;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.BlockExchange
{
    /// <summary>
    ///   Exchange blocks with other peers.
    /// </summary>
    public class BitSwapService : IBitswapService
    {
        private static ILog _log = LogManager.GetLogger(typeof(BitSwapService));

        private readonly ConcurrentDictionary<Cid, WantedBlock> _wants = new ConcurrentDictionary<Cid, WantedBlock>();
        private readonly ConcurrentDictionary<Peer, BitswapLedger> _peerLedgers = new ConcurrentDictionary<Peer, BitswapLedger>();

        /// <summary>
        ///   The supported bitswap protocols.
        /// </summary>
        /// <value>
        ///   Defaults to <see cref="Bitswap11"/> and <see>
        ///       <cref>Bitswap1</cref>
        ///   </see>
        ///   .
        /// </value>
        public IBitswapProtocol[] Protocols { get; set; }

        /// <summary>
        ///   The number of blocks sent by other peers.
        /// </summary>
        private ulong _blocksReceived;

        /// <summary>
        ///   The number of bytes sent by other peers.
        /// </summary>
        private ulong _dataReceived;

        /// <summary>
        ///   The number of blocks sent to other peers.
        /// </summary>
        private ulong _blocksSent;

        /// <summary>
        ///   The number of bytes sent to other peers.
        /// </summary>
        private ulong _dataSent;

        /// <summary>
        ///   The number of duplicate blocks sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        private ulong _dupBlksReceived;

        /// <summary>
        ///   The number of duplicate bytes sent by other peers.
        /// </summary>
        /// <remarks>
        ///   A duplicate block is a block that is already stored in the
        ///   local repository.
        /// </remarks>
        private ulong _dupDataReceived;

        /// <summary>
        ///   Creates a new instance of the <see cref="BitSwapService"/> class.
        /// </summary>
        public BitSwapService(ISwarmService swarmService)
        {
            SwarmService = swarmService;
    
            Protocols = new IBitswapProtocol[]
            {
                new Bitswap11 {BitswapService = this}
            };
        }

        /// <summary>
        ///   Provides access to other peers.
        /// </summary>
        public ISwarmService SwarmService { get; set; }

        /// <summary>
        ///   Provides access to blocks of data.
        /// </summary>
        public IBlockApi BlockService { get; set; }

        /// <summary>
        ///   Statistics on the bitswap component.
        /// </summary>
        /// <seealso cref="IStatsApi"/>
        public BitswapData Statistics
        {
            get
            {
                return new BitswapData
                {
                    BlocksReceived = _blocksReceived,
                    BlocksSent = _blocksSent,
                    DataReceived = _dataReceived,
                    DataSent = _dataSent,
                    DupBlksReceived = _dupBlksReceived,
                    DupDataReceived = _dupDataReceived,
                    ProvideBufLen = 0, // TODO: Unknown meaning
                    Peers = SwarmService.KnownPeers.Select(p => p.Id),
                    Wantlist = _wants.Keys
                };
            }
        }

        /// <summary>
        ///   Gets the bitswap ledger for the specified peer.
        /// </summary>
        /// <param name="peer">
        ///   The peer to get information on.  If the peer is unknown, then a ledger
        ///   with zeros is returned.
        /// </param>
        /// <returns>
        ///   Statistics on the bitswap blocks exchanged with the peer.
        /// </returns>
        /// <seealso cref="IBitSwapApi.GetBitSwapLedger"/>
        public BitswapLedger PeerLedger(Peer peer)
        {
            return _peerLedgers.TryGetValue(peer, out var ledger) ? ledger : new BitswapLedger {Peer = peer};
        }

        /// <summary>
        ///   Raised when a blocked is needed.
        /// </summary>
        /// <remarks>
        ///   Only raised when a block is first requested.
        /// </remarks>
        public event EventHandler<CidEventArgs> BlockNeeded;

        /// <inheritdoc />
        public Task StartAsync()
        {
            _log.Debug("Starting");

            foreach (var protocol in Protocols)
            {
                SwarmService.AddProtocol(protocol);
            }

            SwarmService.ConnectionEstablished += Swarm_ConnectionEstablished;

            // TODO: clear the stats.
            _peerLedgers.Clear();

            return Task.CompletedTask;
        }

        // When a connection is established
        // (1) Send the local peer's want list to the remote
#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Swarm_ConnectionEstablished(object sender, PeerConnection connection)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (_wants.Count == 0)
            {
                return;
            }

            try
            {
                // There is a race condition between getting the remote identity and
                // the remote sending the first wantlist.
                var peer = await connection.IdentityEstablished.Task.ConfigureAwait(false);

                // Fire and forget.
                var _ = SendWantListAsync(peer, _wants.Values, true);
            }
            catch (Exception e)
            {
                _log.Warn("Sending want list", e);
            }
        }

        /// <inheritdoc />
        public Task StopAsync()
        {
            _log.Debug("Stopping");

            SwarmService.ConnectionEstablished -= Swarm_ConnectionEstablished;
            foreach (var protocol in Protocols)
            {
                SwarmService.RemoveProtocol(protocol);
            }

            foreach (var cid in _wants.Keys)
            {
                Unwant(cid);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///   The blocks needed by the peer.
        /// </summary>
        /// <param name="peer">
        ///   The unique ID of the peer.
        /// </param>
        /// <returns>
        ///   The sequence of CIDs need by the <paramref name="peer"/>.
        /// </returns>
        public IEnumerable<Cid> PeerWants(MultiHash peer)
        {
            return _wants.Values
               .Where(w => w.Peers.Contains(peer))
               .Select(w => w.Id);
        }

        /// <summary>
        ///   Adds a block to the want list.
        /// </summary>
        /// <param name="id">
        ///   The CID of the block to add to the want list.
        /// </param>
        /// <param name="peer">
        ///   The unique ID of the peer that wants the block.  This is for
        ///   information purposes only.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result is
        ///   the contents of block.
        /// </returns>
        /// <remarks>
        ///   Other peers are informed that the block is needed by this peer. Hopefully,
        ///   someone will forward it to us.
        ///   <para>
        ///   Besides using <paramref name="cancel"/> for cancellation, the 
        ///   <see cref="Unwant"/> method will also cancel the operation.
        ///   </para>
        /// </remarks>
        public Task<IDataBlock> WantAsync(Cid id, MultiHash peer, CancellationToken cancel)
        {
            if (_log.IsDebugEnabled)
            {
                _log.Debug($"{peer} wants {id}");
            }

            var tsc = new TaskCompletionSource<IDataBlock>();
            var want = _wants.AddOrUpdate(
                id,
                (key) => new WantedBlock
                {
                    Id = id,
                    Consumers = new List<TaskCompletionSource<IDataBlock>> {tsc},
                    Peers = new List<MultiHash> {peer}
                },
                (key, block) =>
                {
                    block.Peers.Add(peer);
                    block.Consumers.Add(tsc);
                    return block;
                }
            );

            // If cancelled, then the block is unwanted.
            cancel.Register(() => Unwant(id));

            // If first time, tell other peers.
            if (want.Consumers.Count != 1)
            {
                return tsc.Task;
            }
            
            var _ = SendWantListToAllAsync(new[] {want}, full: false);
            BlockNeeded?.Invoke(this, new CidEventArgs {Id = want.Id});

            return tsc.Task;
        }

        /// <summary>
        ///   Removes the block from the want list.
        /// </summary>
        /// <param name="id">
        ///   The CID of the block to remove from the want list.
        /// </param>
        /// <remarks>
        ///   Any tasks waiting for the block are cancelled.
        ///   <para>
        ///   No exception is thrown if the <paramref name="id"/> is not
        ///   on the want list.
        ///   </para>
        /// </remarks>
        public void Unwant(Cid id)
        {
            if (_log.IsDebugEnabled)
            {
                _log.Debug($"Unwant {id}");
            }

            if (!_wants.TryRemove(id, out var block))
            {
                return;
            }
            
            foreach (var consumer in block.Consumers)
            {
                consumer.SetCanceled();
            }

            // TODO: Tell the swarm
        }

        /// <summary>
        ///   Indicate that a remote peer sent a block.
        /// </summary>
        /// <param name="remote">
        ///   The peer that sent the block.
        /// </param>
        /// <param name="block">
        ///   The data for the block.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   Updates the statistics.
        ///   </para>
        ///   <para>
        ///   If the block is acceptable then the <paramref name="block"/> is added to local cache
        ///   via the <see cref="BlockService"/>.
        ///   </para>
        /// </remarks>
        public Task OnBlockReceivedAsync(Peer remote, byte[] block)
        {
            return OnBlockReceivedAsync(remote, block, Cid.DefaultContentType, MultiHash.DefaultAlgorithmName);
        }

        /// <summary>
        ///   Indicate that a remote peer sent a block.
        /// </summary>
        /// <param name="remote">
        ///   The peer that sent the block.
        /// </param>
        /// <param name="block">
        ///   The data for the block.
        /// </param>
        /// <param name="contentType">
        ///   The <see cref="Cid.ContentType"/> of the block.
        /// </param>
        /// <param name="multiHash">
        ///   The multihash algorithm name of the block.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///   Updates the statistics.
        ///   </para>
        ///   <para>
        ///   If the block is acceptable then the <paramref name="block"/> is added to local cache
        ///   via the <see cref="BlockService"/>.
        ///   </para>
        /// </remarks>
        public async Task OnBlockReceivedAsync(Peer remote, byte[] block, string contentType, string multiHash)
        {
            // Update statistics.
            ++_blocksReceived;
            _dataReceived += (ulong) block.LongLength;
            _peerLedgers.AddOrUpdate(remote,
                (peer) => new BitswapLedger
                {
                    Peer = peer,
                    BlocksExchanged = 1,
                    DataReceived = (ulong) block.LongLength
                },
                (peer, ledger) =>
                {
                    ++ledger.BlocksExchanged;
                    _dataReceived += (ulong) block.LongLength;
                    return ledger;
                });

            // TODO: Detect if duplicate and update stats
            const bool isDuplicate = false;
            if (isDuplicate)
            {
                ++_dupBlksReceived;
                _dupDataReceived += (ulong) block.Length;
            }

            // TODO: Determine if we should accept the block from the remote.
            const bool acceptable = true;
            if (acceptable)
            {
                await BlockService.PutAsync(
                        data: block,
                        contentType: contentType,
                        multiHash)
                   .ConfigureAwait(false);
            }
        }

        /// <summary>
        ///   Indicate that the local peer sent a block to a remote peer.
        /// </summary>
        /// <param name="remote">
        ///   The peer that sent the block.
        /// </param>
        /// <param name="block">
        ///   The data for the block.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        public Task OnBlockSentAsync(Peer remote, IDataBlock block)
        {
            ++_blocksSent;
            _dataSent += (ulong) block.Size;
            _peerLedgers.AddOrUpdate(remote,
                (peer) => new BitswapLedger
                {
                    Peer = peer,
                    BlocksExchanged = 1,
                    DataSent = (ulong) block.Size
                },
                (peer, ledger) =>
                {
                    ++ledger.BlocksExchanged;
                    _dataSent += (ulong) block.Size;
                    return ledger;
                });

            return Task.CompletedTask;
        }

        /// <summary>
        ///   Indicate that a block is found.
        /// </summary>
        /// <param name="block">
        ///   The block that was found.
        /// </param>
        /// <returns>
        ///   The number of consumers waiting for the <paramref name="block"/>.
        /// </returns>
        /// <remarks>
        ///   <b>Found</b> should be called whenever a new block is discovered. 
        ///   It will continue any Task that is waiting for the block and
        ///   remove the block from the want list.
        /// </remarks>
        public int Found(IDataBlock block)
        {
            if (!_wants.TryRemove(block.Id, out WantedBlock want))
            {
                return 0;
            }
            
            foreach (var consumer in want.Consumers)
            {
                consumer.SetResult(block);
            }

            return want.Consumers.Count;
        }

        /// <summary>
        ///   Send our want list to the connected peers.
        /// </summary>
        private async Task SendWantListToAllAsync(IEnumerable<WantedBlock> wantedBlocks, bool full)
        {
            if (SwarmService == null)
            {
                return;
            }

            try
            {
                var tasks = SwarmService.KnownPeers
                   .Where(p => p.ConnectedAddress != null)
                   .Select(p => SendWantListAsync(p, wantedBlocks, full))
                   .ToArray();
                if (_log.IsDebugEnabled)
                {
                    _log.Debug($"Spamming {tasks.Count()} connected peers");
                }
                
                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (_log.IsDebugEnabled)
                {
                    _log.Debug($"Spam {tasks.Count()} connected peers done");
                }
            }
            catch (Exception e)
            {
                _log.Debug("sending to all failed", e);
            }
        }

        private async Task SendWantListAsync(Peer peer, IEnumerable<WantedBlock> wants, bool full)
        {
            _log.Debug($"sending want list to {peer}");

            // Send the want list to the peer on any bitswap protocol
            // that it supports.
            foreach (var protocol in Protocols)
            {
                try
                {
                    await using (var stream = await SwarmService.DialAsync(peer, protocol.ToString()).ConfigureAwait(false))
                    {
                        await protocol.SendWantsAsync(stream, wants, full: full).ConfigureAwait(false);
                    }

                    return;
                }
                catch (Exception)
                {
                    _log.Debug($"{peer} refused {protocol}");
                }
            }

            _log.Warn($"{peer} does not support any bitswap protocol");
        }
    }
}
