using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Common.Logging;
using Lib.P2P;
using MultiFormats;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class SwarmApi : ISwarmApi
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SwarmApi));
        private readonly ISwarmService _swarmService;

        private static readonly MultiAddress[] DefaultFilters = { };
        private readonly IConfigApi _configApi;

        public SwarmApi(SwarmService swarmService, IConfigApi configApi)
        {
            _swarmService = swarmService;
            _configApi = configApi;
        }

        public async Task<MultiAddress> AddAddressFilterAsync(MultiAddress address,
            bool persist = false,
            CancellationToken cancel = default)
        {
            var addrs = (await ListAddressFiltersAsync(persist, cancel).ConfigureAwait(false)).ToList();
            if (addrs.Any(a => a == address))
            {
                return address;
            }

            addrs.Add(address);
            var strings = addrs.Select(a => a.ToString());
            await _configApi.SetAsync("Swarm.AddrFilters", JToken.FromObject(strings), cancel).ConfigureAwait(false);

            _swarmService.WhiteList.Add(address);

            return address;
        }

        public async Task<IEnumerable<Peer>> AddressesAsync(CancellationToken cancel = default)
        {
            return _swarmService.KnownPeers;
        }

        public async Task ConnectAsync(MultiAddress address, CancellationToken cancel = default)
        {
            Log.Debug($"Connecting to {address}");
            var conn = await _swarmService.ConnectAsync(address, cancel).ConfigureAwait(false);
            Log.Debug($"Connected to {conn.RemotePeer.ConnectedAddress}");
        }

        public async Task ConnectAsync(Peer address, CancellationToken cancel = default) 
        { 
            Log.Debug($"Connecting to {address}");
            var conn = await _swarmService.ConnectAsync(address, cancel).ConfigureAwait(false);
            Log.Debug($"Connected to {conn.RemotePeer.ConnectedAddress}");  
        }

        public async Task DisconnectAsync(MultiAddress address, CancellationToken cancel = default)
        {
            await _swarmService.DisconnectAsync(address, cancel).ConfigureAwait(false);
        }

        public async Task<IEnumerable<MultiAddress>> ListAddressFiltersAsync(bool persist = false,
            CancellationToken cancel = default)
        {
            try
            {
                var json = await _configApi.GetAsync("Swarm.AddrFilters", cancel).ConfigureAwait(false);
                return json == null ? new MultiAddress[0] : json.Select(a => MultiAddress.TryCreate((string) a)).Where(a => a != null);
            }
            catch (KeyNotFoundException)
            {
                var strings = DefaultFilters.Select(a => a.ToString());
                await _configApi.SetAsync("Swarm.AddrFilters", JToken.FromObject(strings), cancel)
                   .ConfigureAwait(false);
                
                return DefaultFilters;
            }
        }

        public async Task<IEnumerable<Peer>> PeersAsync(CancellationToken cancel = default)
        {
            return _swarmService.KnownPeers.Where(p => p.ConnectedAddress != null);
        }

        public async Task<MultiAddress> RemoveAddressFilterAsync(MultiAddress address,
            bool persist = false,
            CancellationToken cancel = default)
        {
            var addrs = (await ListAddressFiltersAsync(persist, cancel).ConfigureAwait(false)).ToList();
            if (addrs.All(a => a != address))
            {
                return null;
            }

            addrs.Remove(address);
            var strings = addrs.Select(a => a.ToString());
            await _configApi.SetAsync("Swarm.AddrFilters", JToken.FromObject(strings), cancel).ConfigureAwait(false);

            _swarmService.WhiteList.Remove(address);

            return address;
        }
    }
}
