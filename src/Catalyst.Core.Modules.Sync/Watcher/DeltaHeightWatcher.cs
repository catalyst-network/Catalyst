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
using Catalyst.Abstractions.P2P.Repository;
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
        private readonly IPeerRepository _peerRepository;
        private readonly ILibP2PPeerService _peerService;
        private readonly ILibP2PPeerClient _peerClient;
        private readonly ICatalystProtocol _catalystProtocol;
        private readonly ISwarmApi _swarmApi;

        public DeltaIndex LatestDeltaHash { set; get; }

        private Timer _requestDeltaHeightTimer;
        private ManualResetEventSlim _manualResetEventSlim;
        private readonly int _peersPerCycle = 50;
        private readonly int _minimumPeers;

        public DeltaHeightWatcher(ILibP2PPeerClient peerClient,
            ISwarmApi swarmApi,
            IPeerRepository peerRepository,
            ILibP2PPeerService peerService,
            double threshold = 0.5d,
            int minimumPeers = 0)
        {
            _peerClient = peerClient;
            DeltaHeightRanker = new DeltaHeightRanker(peerRepository, 100, threshold);
            _peerRepository = peerRepository;
            _peerService = peerService;
            _manualResetEventSlim = new ManualResetEventSlim(false);
            _swarmApi = swarmApi;

            _threshold = threshold;
            _minimumPeers = minimumPeers;
        }

        private int _page;
        private bool _hasLooped = false;

        public void RequestDeltaHeightTimerCallback(object state)
        {
            //Can improve this later when networking layers get reduced
            var peerCount = DeltaHeightRanker.GetPeers().Count();
            if (_hasLooped && peerCount >= _minimumPeers)
            {
                _manualResetEventSlim.Set();
            }

            RequestDeltaHeightFromPeers();
        }

        private int GetPageCount()
        {
            var peerCount = _peerRepository.Count();
            if (peerCount == 0)
            {
                return 1;
            }

            var pages = (int) Math.Ceiling(peerCount / (decimal) _peersPerCycle);
            return pages;
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
            var totalPages = GetPageCount();
            _page %= totalPages;
            _page++;

            var peers = await _swarmApi.PeersAsync();
            await _peerClient.SendMessageToPeersAsync(new LatestDeltaHashRequest(), peers.Select(x => x.ConnectedAddress)).ConfigureAwait(false);

            if (_page >= totalPages && DeltaHeightRanker.GetPeers().Count() >= _minimumPeers)
            {
                _hasLooped = true;
            }
        }

        private void DeltaHeightOnNext(ProtocolMessage protocolMessage)
        {
            var peerId = protocolMessage.PeerId;
            var latestDeltaHash = protocolMessage.FromProtocolMessage<LatestDeltaHashResponse>();

            var peer = _peerRepository.Get(peerId);
            if (peer != null)
            {
                peer.Touch();
                _peerRepository.Update(peer);
            }

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
