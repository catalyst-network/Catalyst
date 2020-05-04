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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Lib.P2P;
using Lib.P2P;
using Lib.P2P.Routing;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    public sealed class DhtApi : IDhtApi
    {
        private readonly IDhtService _dhtService;

        public DhtApi(KatDhtService dhtService)
        {
            _dhtService = dhtService;
        }

        public async Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default)
        {
            return await _dhtService.FindPeerAsync(id, cancel).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id,
            int limit = 20,
            Action<Peer> providerFound = null,
            CancellationToken cancel = default)
        {
            return await _dhtService.FindProvidersAsync(id, limit, providerFound, cancel).ConfigureAwait(false);
        }

        public async Task ProvideAsync(Cid cid,
            bool advertise = true,
            CancellationToken cancel = default)
        {
            await _dhtService.ProvideAsync(cid, advertise, cancel).ConfigureAwait(false);
        }

        public Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task PutAsync(byte[] key, out byte[] value, CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryGetAsync(byte[] key,
            out byte[] value,
            CancellationToken cancel = default)
        {
            throw new NotImplementedException();
        }
    }
}
