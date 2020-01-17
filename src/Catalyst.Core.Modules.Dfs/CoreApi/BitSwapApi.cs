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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Lib.P2P;
using MultiFormats;
using Nito.AsyncEx;
using Serilog;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class BitSwapApi : IBitSwapApi
    {
        private readonly IBitswapService _bitSwapService;
        private AsyncLazy<Peer> LocalPeer { get; }

        public BitSwapApi(BitSwapService bitSwapService, IKeyApi keyApi)
        {
            _bitSwapService = bitSwapService;
            
            LocalPeer = new AsyncLazy<Peer>(async () =>
            {
                Log.Debug("Building local peer");
                Log.Debug("Getting key info about self");
                var self = await keyApi.GetPublicKeyAsync("self").ConfigureAwait(false);
                var localPeer = new Peer
                {
                    Id = self.Id,
                    PublicKey = keyApi.GetPublicKeyAsync("self").ConfigureAwait(false).GetAwaiter().GetResult().Id.ToString(),
                    ProtocolVersion = "ipfs/0.1.0"
                };
                var version = typeof(DfsService).GetTypeInfo().Assembly.GetName().Version;
                localPeer.AgentVersion = $"net-ipfs/{version.Major}.{version.Minor}.{version.Revision}";
                Log.Debug("Built local peer");
                return localPeer;
            });
        }

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default)
        {
            var peer = await LocalPeer.ConfigureAwait(false);
            var dataBlock = await _bitSwapService.WantAsync(id, peer.Id, cancel).ConfigureAwait(false);
            return dataBlock;
        }

        public BitswapLedger GetBitSwapLedger(Peer peer, CancellationToken cancel = default)
        {
            try
            {
                return _bitSwapService.PeerLedger(peer);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        public int FoundBlock(IDataBlock block) { return _bitSwapService.Found(block); }

        public BitswapData GetBitSwapStatistics() { return _bitSwapService.Statistics; }

        public void UnWant(Cid id, CancellationToken cancel = default)
        {
            _bitSwapService.Unwant(id);
        }

        public async Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null,
            CancellationToken cancel = default)
        {
            if (peer == null)
            {
                peer = (await LocalPeer.ConfigureAwait(false)).Id;
            }

            return _bitSwapService.PeerWants(peer);
        }
    }
}
