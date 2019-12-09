using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using Lib.P2P.Transports;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class StatsApi : IStatsApi
    {
        private readonly IBitswapApi _bitSwapApi;
        private readonly IBlockRepositoryApi _blockRepositoryApi;

        public StatsApi(IBitswapApi bitSwapApi, IBlockRepositoryApi blockRepositoryApi)
        {
            _bitSwapApi = bitSwapApi;
            _blockRepositoryApi = blockRepositoryApi;
        }

        public Task<BandwidthData> BandwidthAsync(CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult(StatsStream.AllBandwidth);
        }

        public async Task<BitswapData> BitSwapAsync(CancellationToken cancel = default(CancellationToken))
        {
            return _bitSwapApi.GetBitSwapStatistics();
        }

        public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default(CancellationToken))
        {
            return _blockRepositoryApi.StatisticsAsync(cancel);
        }
    }
}
