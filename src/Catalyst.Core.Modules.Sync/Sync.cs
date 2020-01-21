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
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Wire;
using Lib.P2P;

namespace Catalyst.Core.Modules.Sync
{
    public class Sync
    {
        private const int PeerCount = 10;
        private readonly int _rangeSize;
        private Cid _latestDeltaHash;
        private readonly IPeerSettings _peerSettings;
        private readonly ILedger _ledger;
        private readonly IPeerClient _peerClient;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerClientObservable _deltaHeightResponseObserver;
        private readonly IPeerClientObservable _deltaHistoryResponseObserver;
        private readonly IMapperProvider _mapperProvider;

        private IDisposable _deltaHeightSubscription;
        private IDisposable _deltaHistorySubscription;

        public int CurrentDeltaIndex { private set; get; }
        public bool IsSynchronized { private set; get; }

        public Sync(IPeerSettings peerSettings,
            ILedger ledger,
            IPeerClient peerClient,
            IDeltaIndexService deltaIndexService,
            IPeerRepository peerRepository,
            IPeerClientObservable deltaHeightResponseObserver,
            IPeerClientObservable deltaHistoryResponseObserver,
            IMapperProvider mapperProvider,
            int rangeSize = 100)
        {
            _rangeSize = rangeSize;
            _peerSettings = peerSettings;
            _ledger = ledger;
            _peerClient = peerClient;
            _deltaIndexService = deltaIndexService;
            _peerRepository = peerRepository;
            _deltaHeightResponseObserver = deltaHeightResponseObserver;
            _deltaHistoryResponseObserver = deltaHistoryResponseObserver;
            _mapperProvider = mapperProvider;
        }

        public void Start()
        {
            _deltaHeightSubscription = _deltaHeightResponseObserver.MessageStream.Subscribe(DeltaHeightOnNext);
            _deltaHistorySubscription = _deltaHistoryResponseObserver.MessageStream.Subscribe(DeltaHistoryOnNext);

            Progress();
        }

        public void Stop()
        {
            _deltaHeightSubscription.Dispose();
            _deltaHistorySubscription.Dispose();
        }

        private void Progress()
        {
            CurrentDeltaIndex = _deltaIndexService.Height();

            var deltaHistoryRequest = new DeltaHistoryRequest
                {Height = (uint) CurrentDeltaIndex, Range = (uint) _rangeSize};
            var protocolMessage = deltaHistoryRequest.ToProtocolMessage(_peerSettings.PeerId);
            SendToRandomPeers(protocolMessage);
        }

        private void SendToRandomPeers(ProtocolMessage protocolMessage)
        {
            var peers = _peerRepository.GetActivePeers(PeerCount);
            foreach (var peer in peers)
            {
                _peerClient.SendMessage(new MessageDto(
                    protocolMessage,
                    peer.PeerId));
            }
        }

        private void GetDeltaHeight()
        {
            var deltaHeightRequest = new LatestDeltaHashRequest();
            var protocolMessage = deltaHeightRequest.ToProtocolMessage(_peerSettings.PeerId);
            SendToRandomPeers(protocolMessage);
        }

        private void DeltaHistoryOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            if (!(peerClientMessageDto.Message is DeltaHistoryResponse deltaHistoryResponse))
            {
                return;
            }

            var deltaIndexes = deltaHistoryResponse.Result
               .Select(x => x.ToDao<DeltaIndex, DeltaIndexDao>(_mapperProvider)).OrderBy(x => x.Height);

            UpdateState(deltaIndexes);

            Progress();
        }

        private void UpdateState(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            foreach (var deltaIndex in deltaIndexes)
            {
                _ledger.Update(deltaIndex.Cid);
                _deltaIndexService.Add(deltaIndex);
                CurrentDeltaIndex = deltaIndex.Height;

                if (_latestDeltaHash == deltaIndex.Cid)
                {
                    IsSynchronized = true;
                }
            }
        }

        private void DeltaHeightOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            if (!(peerClientMessageDto.Message is LatestDeltaHashResponse latestDeltaHashResponse))
            {
                return;
            }

            _latestDeltaHash = latestDeltaHashResponse.DeltaHash.ToByteArray().ToCid();
        }
    }
}
