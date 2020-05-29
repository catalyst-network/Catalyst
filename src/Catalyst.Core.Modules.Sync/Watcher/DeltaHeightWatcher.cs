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
using Lib.P2P.Protocols;

namespace Catalyst.Core.Modules.Sync.Watcher
{
    public class DeltaHeightWatcher : IDeltaHeightWatcher
    {
        private bool _disposed;
        private readonly double _threshold;
        public IDeltaHeightRanker DeltaHeightRanker { private set; get; }
        private IDisposable _deltaHeightSubscription;
        private readonly ILibP2PPeerService _peerService;
        private readonly ILibP2PPeerClient _peerClient;
        private readonly ISwarmApi _swarmApi;

        public DeltaIndex LatestDeltaHash { set; get; }

        private Timer _requestDeltaHeightTimer;
        private ManualResetEventSlim _manualResetEventSlim;

        public DeltaHeightWatcher(ILibP2PPeerClient peerClient,
            ISwarmApi swarmApi,
            ILibP2PPeerService peerService,
            double threshold = 0.5d)
        {
            _peerClient = peerClient;
            DeltaHeightRanker = new DeltaHeightRanker(swarmApi, 100, threshold);
            _peerService = peerService;
            _manualResetEventSlim = new ManualResetEventSlim(false);
            _swarmApi = swarmApi;

            _threshold = threshold;
        }

        public async void RequestDeltaHeightTimerCallback(object state)
        {
            var connectedPeers = _swarmApi.PeersAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var peerCount = DeltaHeightRanker.GetPeers().Count();
            var confirmCount = connectedPeers.Count();
            if (confirmCount > 0) confirmCount /= 2;
            if (peerCount >= confirmCount)
            {
                _manualResetEventSlim.Set();
            }

            await RequestDeltaHeightFromPeers();
        }

        public async Task<DeltaIndex> GetHighestDeltaIndexAsync()
        {
            _manualResetEventSlim.Wait();
            var highestDeltaIndex = GetMostPopularMessage()?.Item.DeltaIndex;
            return highestDeltaIndex;
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
            var peerId = protocolMessage.PeerId;
            var latestDeltaHash = protocolMessage.FromProtocolMessage<LatestDeltaHashResponse>();
            DeltaHeightRanker.Add(peerId, latestDeltaHash);
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
