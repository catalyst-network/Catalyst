#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Catalyst.Core.Modules.Dfs.BlockExchange;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class BitSwapApi : IBitSwapApi
    {
        private readonly IBitswapService _bitSwapService;
        private readonly Peer _localPeer;

        public BitSwapApi(BitSwapService bitSwapService, IKeyApi keyApi, Peer localPeer)
        {
            _bitSwapService = bitSwapService;
            _localPeer = localPeer;
        }

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default)
        {
            var dataBlock = await _bitSwapService.WantAsync(id, _localPeer.Id, cancel).ConfigureAwait(false);
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

        public void UnWant(Cid id, CancellationToken cancel = default) { _bitSwapService.Unwant(id); }

        public async Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null,
            CancellationToken cancel = default)
        {
            if (peer == null)
            {
                peer = _localPeer.Id;
            }

            return _bitSwapService.PeerWants(peer);
        }
    }
}
