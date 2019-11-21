using System.Threading;
using System.Threading.Tasks;
using Ipfs.Abstractions;
using Ipfs.Abstractions.CoreApi;
using PeerTalk;

namespace Ipfs.Core.CoreApi
{
    internal class StatsApi : IStatsApi
    {
        private readonly IpfsEngine _ipfs;

        public StatsApi(IpfsEngine ipfs) { this._ipfs = ipfs; }

        public Task<BandwidthData> BandwidthAsync(CancellationToken cancel = default)
        {
            return Task.FromResult<BandwidthData>(StatsStream.AllBandwidth);
        }

        public async Task<BitswapData> BitswapAsync(CancellationToken cancel = default)
        {
            var bitswap = await _ipfs.BitswapService.ConfigureAwait(false);
            return bitswap.Statistics;
        }

        public Task<RepositoryData> RepositoryAsync(CancellationToken cancel = default)
        {
            return _ipfs.BlockRepository.StatisticsAsync(cancel);
        }
    }
}
