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
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Wire;
using Nethermind.Core.Extensions;

namespace Catalyst.Core.Modules.Sync.Watcher
{
    public class DeltaHeightWatcher : IDeltaHeightWatcher
    {
        private bool _disposed;
        public IDeltaHeightRanker DeltaHeightRanker { private set; get; }
        private IDisposable _deltaHeightSubscription;
        private readonly IPeerService _peerService;
        private readonly IPeerClient _peerClient;
        private readonly ISwarmApi _swarmApi;

        public DeltaIndex LatestDeltaHash { set; get; }

        private Timer _requestDeltaHeightTimer;
        private readonly AutoResetEvent _autoResetEvent;

        public DeltaHeightWatcher(IPeerClient peerClient,
            ISwarmApi swarmApi,
            IPeerService peerService,
            double threshold = 0.5d)
        {
            _peerClient = peerClient;
            DeltaHeightRanker = new DeltaHeightRanker(swarmApi, 100, threshold);
            _peerService = peerService;
            _autoResetEvent = new AutoResetEvent(false);
            _swarmApi = swarmApi;
        }

        public async void RequestDeltaHeightTimerCallback(object state)
        {
            await RequestDeltaHeightFromPeers().ConfigureAwait(false);
        }

        public async Task<DeltaIndex> WaitForDeltaIndexAsync(TimeSpan timeout)
        {
            return await WaitForDeltaIndexAsync(timeout, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<DeltaIndex> WaitForDeltaIndexAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            await _autoResetEvent.WaitOneAsync(timeout, cancellationToken);
            return await GetHighestDeltaIndexAsync().ConfigureAwait(false);
        }

        public Task<DeltaIndex> GetHighestDeltaIndexAsync()
        {
            return Task.FromResult(GetMostPopularMessage()?.Item.DeltaIndex);
        }

        private IRankedItem<LatestDeltaHashResponse> GetMostPopularMessage()
        {
            //Responses that have fully sync
            var rankedResponses = DeltaHeightRanker.GetMessagesByMostPopular();
            var highestRankedSyncResponse = rankedResponses.Where(x => x.Item.IsSync).FirstOrDefault();
            if (highestRankedSyncResponse != null)
            {
                return highestRankedSyncResponse;
            }

            //Responses that have not fully sync
            var highestRankedUnSyncResponse = rankedResponses.FirstOrDefault();
            if (highestRankedUnSyncResponse != null)
            {
                return highestRankedUnSyncResponse;
            }

            return null;
        }

        private async Task RequestDeltaHeightFromPeers()
        {
            var connectedPeers = await _swarmApi.PeersAsync().ConfigureAwait(false);
            await _peerClient.SendMessageToPeersAsync(new LatestDeltaHashRequest(), connectedPeers.Select(x => x.ConnectedAddress)).ConfigureAwait(false);
        }

        private void DeltaHeightOnNext(ProtocolMessage protocolMessage)
        {
            var address = protocolMessage.Address;
            var latestDeltaHash = protocolMessage.FromProtocolMessage<LatestDeltaHashResponse>();
            DeltaHeightRanker.Add(address, latestDeltaHash);
        }

        public void Start()
        {
            _deltaHeightSubscription = _peerService.MessageStream.Where(x => x != null &&
                    x.Payload.TypeUrl.EndsWith(typeof(LatestDeltaHashResponse).ShortenedProtoFullName()))
               .Select(x => x.Payload)
               .Subscribe(DeltaHeightOnNext);

            _requestDeltaHeightTimer = new Timer(RequestDeltaHeightTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public void Stop() { CleanUp(); }

        private void CleanUp()
        {
            _deltaHeightSubscription?.Dispose();
            _requestDeltaHeightTimer?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanUp();
                }
            }
            _disposed = true;
        }
    }
}
