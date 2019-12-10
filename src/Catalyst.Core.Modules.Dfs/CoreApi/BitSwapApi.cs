using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.BlockExchange;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.Keystore;
using Lib.P2P;
using MultiFormats;
using Nito.AsyncEx;
using Serilog;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class BitSwapApi : IBitSwapApi
    {
        private readonly IBitswapService _bitSwapService;
        private AsyncLazy<Peer> LocalPeer { get; set; }

        public BitSwapApi(IBitswapService bitSwapService, IKeyApi keyApi)
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

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            var peer = await LocalPeer.ConfigureAwait(false);
            return await _bitSwapService.WantAsync(id, peer.Id, cancel).ConfigureAwait(false);
        }

        public async Task<BitswapLedger> LedgerAsync(Peer peer, CancellationToken cancel = default)
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

        public async Task UnWantAsync(Cid id, CancellationToken cancel = default)
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
