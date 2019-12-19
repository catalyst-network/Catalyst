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

        public async Task<Peer> FindPeerAsync(MultiHash id, CancellationToken cancel = default(CancellationToken))
        {
            return await _dhtService.FindPeerAsync(id, cancel).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Peer>> FindProvidersAsync(Cid id,
            int limit = 20,
            Action<Peer> providerFound = null,
            CancellationToken cancel = default(CancellationToken))
        {
            return await _dhtService.FindProvidersAsync(id, limit, providerFound, cancel).ConfigureAwait(false);
        }

        public async Task ProvideAsync(Cid cid,
            bool advertise = true,
            CancellationToken cancel = default(CancellationToken))
        {
            await _dhtService.ProvideAsync(cid, advertise, cancel).ConfigureAwait(false);
        }

        public Task<byte[]> GetAsync(byte[] key, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task PutAsync(byte[] key, out byte[] value, CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<bool> TryGetAsync(byte[] key,
            out byte[] value,
            CancellationToken cancel = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
