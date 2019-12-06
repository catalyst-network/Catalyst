using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using Lib.P2P.Transports;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    class StatsApi : IStatsApi
    {
        IDfs ipfs;

        public StatsApi(IDfs ipfs) { this.ipfs = ipfs; }

        public Task<BandwidthData> BandwidthAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(StatsStream.AllBandwidth);
        }

        public async Task<BitswapData> BitswapAsync(CancellationToken cancel = default(CancellationToken))
        {
            var bitswap = await ipfs.BitswapService.ConfigureAwait(false);
            return bitswap.Statistics;
        }

        public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            return ipfs.BlockRepository.StatisticsAsync(cancel);
        }
    }
}
