using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    class BitswapApi : IBitswapApi
    {
        IDfs ipfs;

        public BitswapApi(IDfs ipfs) { this.ipfs = ipfs; }

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var bs = await ipfs.BitswapService.ConfigureAwait(false);
            var peer = await ipfs.LocalPeer.ConfigureAwait(false);
            return await bs.WantAsync(id, peer.Id, cancel).ConfigureAwait(false);
        }

        public async Task<BitswapLedger> LedgerAsync(Peer peer, CancellationToken cancel = default(CancellationToken))
        {
            var bs = await ipfs.BitswapService.ConfigureAwait(false);
            return bs.PeerLedger(peer);
        }

        public async Task UnwantAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            (await ipfs.BitswapService.ConfigureAwait(false)).Unwant(id);
        }

        public async Task<IEnumerable<Cid>> WantsAsync(MultiHash peer = null,
            CancellationToken cancel = default(CancellationToken))
        {
            if (peer == null)
            {
                peer = (await ipfs.LocalPeer.ConfigureAwait(false)).Id;
            }

            var bs = await ipfs.BitswapService.ConfigureAwait(false);
            return bs.PeerWants(peer);
        }
    }
}
