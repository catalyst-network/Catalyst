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
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Sync.Extensions;
using Catalyst.Core.Modules.Sync.Manager;
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;

namespace Catalyst.Core.Modules.Sync.Watcher
{
    public class DeltaHeightWatcher : IDeltaHeightWatcher
    {
        private bool _disposed;
        private readonly double _threshold;
        public IDeltaHeightRanker DeltaHeightRanker { private set; get; }
        private IDisposable _deltaHeightSubscription;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerService _peerService;
        private readonly IMessenger _messenger;

        public DeltaIndex LatestDeltaHash { set; get; }

        private Timer _requestDeltaHeightTimer;
        private ManualResetEventSlim _manualResetEventSlim;
        private readonly int _peersPerCycle = 50;

        public DeltaHeightWatcher(IMessenger messenger,
            IPeerRepository peerRepository,
            IPeerService peerService,
            double threshold = 0.5d)
        {
            _messenger = messenger;
            DeltaHeightRanker = new DeltaHeightRanker(peerRepository, 100, threshold);
            _peerRepository = peerRepository;
            _peerService = peerService;
            _manualResetEventSlim = new ManualResetEventSlim(false);
            _threshold = threshold;
        }

        private int _page;
        private bool _hasLooped = false;

        public void RequestDeltaHeightTimerCallback(object state)
        {
            var acceptanceThreshold = (_peersPerCycle * 0.5);
            if (_hasLooped || GetMostPopularMessage()?.Score > acceptanceThreshold)
            {
                _manualResetEventSlim.Set();
            }

            RequestDeltaHeightFromPeers();
        }

        private int GetPages()
        {
            var peerCount = _peerRepository.Count();
            var pages = (int) Math.Ceiling(peerCount / (decimal)_peersPerCycle);
            return pages;
        }

        public async Task<DeltaIndex> GetHighestDeltaIndexAsync()
        {
            _manualResetEventSlim.Wait();
            return GetMostPopularMessage()?.Item.DeltaIndex;
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

        private void RequestDeltaHeightFromPeers()
        {
            var totalPages = GetPages();
            _page %= totalPages;
            _page++;
            var peers = DeltaHeightRanker.GetPeers().Union(_peerRepository.TakeHighestReputationPeers(_page, _peersPerCycle).Select(x => x.PeerId));
            _messenger.SendMessageToPeers(new LatestDeltaHashRequest(), peers);

            if (_page == totalPages)
            {
                _hasLooped = true;
            }
        }

        private void DeltaHeightOnNext(ProtocolMessage protocolMessage)
        {
            DeltaHeightRanker.Add(protocolMessage.PeerId, protocolMessage.FromProtocolMessage<LatestDeltaHashResponse>());
        }

        public void Start()
        {
            _deltaHeightSubscription = _peerService.MessageStream.Where(x => x.Payload != null &&
                    x.Payload.TypeUrl.EndsWith(typeof(LatestDeltaHashResponse).ShortenedProtoFullName()))
               .Select(x => x.Payload)
               .Subscribe(DeltaHeightOnNext);

            _requestDeltaHeightTimer = new Timer(RequestDeltaHeightTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public void Stop() { CleanUp(); }

        private void CleanUp()
        {
            _deltaHeightSubscription.Dispose();
            _requestDeltaHeightTimer.Dispose();
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
