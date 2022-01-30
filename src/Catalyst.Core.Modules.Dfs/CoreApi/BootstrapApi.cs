#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Options;
using MultiFormats;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    public class BootstrapApi : IBootstrapApi
    {
        // From https://github.com/libp2p/go-libp2p-daemon/blob/master/bootstrap.go#L14
        public static MultiAddress[] Defaults =
        {
            "/ip4/77.68.110.194/tcp/4001/ipfs/QmX3Ye8zfH1u46pyzWAeJjCLbH4XTVA89LGYK6fZsPFAkw",
            "/ip4/77.68.110.195/tcp/4001/ipfs/QmVgGUKQw9FFX5iqKK2ZxHQtSfpDNiSiwUfykWrLQkBagK",
            "/ip4/77.68.110.196/tcp/4001/ipfs/QmU9yjJLChQucWkjAiKD5HkMCoPdoo3ndP1kkkDqWUKeDN",
            "/ip4/77.68.110.197/tcp/4001/ipfs/QmZf3ARncMmSZoNm5QZm4QXPsTjLJpUDKHsEQEhoPTwzuf"

            //"/ip4/192.168.1.45/tcp/4001/ipfs/QmUUNAUD5YLrCZ4vBn8WsxTbMjtgJYkAkAYiFyoEdb3edu",
            //"/ip4/192.168.1.46/tcp/4001/ipfs/QmNNdTXfLRqo4Puc6JPGq2o3xDBcJhxDXCbThuzDV6nRP1",
            //"/ip4/192.168.1.47/tcp/4001/ipfs/QmQVoUpHf3yveqcrF2cFTWgutrSFo1Cm1CSffqKQZ52WHL",
            //"/ip4/192.168.1.233/tcp/4001/ipfs/QmPDpc3KHGtomZjAsfuw7ZaYrahVNBtXX8Kjoy6cCMRLp7",
            //"/ip4/192.168.1.40/tcp/4001/ipfs/QmbRNTx28U6Wptthtog4vwXF5QtMyTfZdEX56MCFFPdZHB",
            //"/ip4/192.168.1.232/tcp/4001/ipfs/QmaZtpXfM713jTpLACJ2njMm7Qi4D2FNrAjZKM1e6L9bLM"
        };

        private readonly IConfigApi _configApi;
        private readonly DiscoveryOptions _discoveryOptions;

        public BootstrapApi(IConfigApi configApi, DiscoveryOptions discoveryOptions)
        {
            _configApi = configApi;
            _discoveryOptions = discoveryOptions;
        }

        public async Task<MultiAddress> AddAsync(MultiAddress address,
            CancellationToken cancel = default)
        {
            // Throw if missing peer ID
            var _ = address.PeerId;

            var addrs = (await ListAsync(cancel).ConfigureAwait(false)).ToList();
            if (addrs.Any(a => a == address))
            {
                return address;
            }

            addrs.Add(address);
            var strings = addrs.Select(a => a.ToString());
            await _configApi.SetAsync("Bootstrap", JToken.FromObject(strings), cancel).ConfigureAwait(false);
            return address;
        }

        public async Task<IEnumerable<MultiAddress>> AddDefaultsAsync(CancellationToken cancel =
            default)
        {
            foreach (var a in Defaults)
            {
                await AddAsync(a, cancel).ConfigureAwait(false);
            }

            return Defaults;
        }

        public async Task<IEnumerable<MultiAddress>> ListAsync(CancellationToken cancel = default)
        {
            if (_discoveryOptions.BootstrapPeers != null)
            {
                return _discoveryOptions.BootstrapPeers;
            }

            try
            {
                var json = await _configApi.GetAsync("Bootstrap", cancel);
                return json == null ? new MultiAddress[0] : json.Select(a => MultiAddress.TryCreate((string)a)).Where(a => a != null);
            }
            catch (KeyNotFoundException)
            {
                var strings = Defaults.Select(a => a.ToString());
                await _configApi.SetAsync("Bootstrap", JToken.FromObject(strings), cancel).ConfigureAwait(false);
                return Defaults;
            }
        }

        public async Task RemoveAllAsync(CancellationToken cancel = default)
        {
            await _configApi.SetAsync("Bootstrap", JToken.FromObject(new string[0]), cancel).ConfigureAwait(false);
        }

        public async Task<MultiAddress> RemoveAsync(MultiAddress address,
            CancellationToken cancel = default)
        {
            var addrs = (await ListAsync(cancel).ConfigureAwait(false)).ToList();
            if (addrs.All(a => a != address))
            {
                return address;
            }

            addrs.Remove(address);
            var strings = addrs.Select(a => a.ToString());
            await _configApi.SetAsync("Bootstrap", JToken.FromObject(strings), cancel).ConfigureAwait(false);
            return address;
        }
    }
}
