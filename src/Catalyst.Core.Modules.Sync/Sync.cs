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
using System.Threading.Tasks;
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Google.Protobuf;

namespace Catalyst.Core.Modules.Sync
{
    public interface IDeltaHeightWatcher
    {
        DeltaIndexDao LatestDeltaHash { get; }
        Task StartAsync();
        Task StopAsync();
        Task WaitForDeltaHeightAsync(int currentDeltaIndex);
    }

    public interface IPeerSyncManager
    {
        void SendToRandomPeers(IMessage message);
    }

    public class PeerSyncManager : IPeerSyncManager
    {
        private const int PeerCount = 10;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerClient _peerClient;
        private readonly IPeerRepository _peerRepository;

        public PeerSyncManager(IPeerSettings peerSettings,
            IPeerClient peerClient,
            IPeerRepository peerRepository)
        {
            _peerSettings = peerSettings;
            _peerClient = peerClient;
            _peerRepository = peerRepository;
        }

        public void SendToRandomPeers(IMessage message)
        {
            var protocolMessage = message.ToProtocolMessage(_peerSettings.PeerId);
            var peers = _peerRepository.GetActivePeers(PeerCount);
            foreach (var peer in peers)
            {
                _peerClient.SendMessage(new MessageDto(
                    protocolMessage,
                    peer.PeerId));
            }
        }
    }

    public class DeltaHeightWatcher : IDeltaHeightWatcher
    {
        public DeltaIndexDao LatestDeltaHash { private set; get; }
        private readonly IMapperProvider _mapperProvider;

        private readonly IPeerSyncManager _peerSyncManager;
        private IDisposable _deltaHeightSubscription;
        private readonly IPeerClientObservable _deltaHeightResponseObserver;

        public DeltaHeightWatcher(IPeerSyncManager peerSyncManager,
            IPeerClientObservable deltaHeightResponseObserver,
            IMapperProvider mapperProvider)
        {
            _peerSyncManager = peerSyncManager;
            _deltaHeightResponseObserver = deltaHeightResponseObserver;
            _mapperProvider = mapperProvider;
        }

        public async Task StartAsync()
        {
            _deltaHeightSubscription = _deltaHeightResponseObserver.MessageStream.Subscribe(DeltaHeightOnNext);
        }

        public async Task StopAsync() { _deltaHeightSubscription.Dispose(); }

        public async Task WaitForDeltaHeightAsync(int currentDeltaIndex)
        {
            while (LatestDeltaHash == null || LatestDeltaHash.Height == currentDeltaIndex)
            {
                var deltaHeightRequest = new LatestDeltaHashRequest();
                _peerSyncManager.SendToRandomPeers(deltaHeightRequest);
                await Task.Delay(10000);
            }
        }

        private void DeltaHeightOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            if (!(peerClientMessageDto.Message is LatestDeltaHashResponse latestDeltaHashResponse))
            {
                return;
            }

            LatestDeltaHash = latestDeltaHashResponse.Result.ToDao<DeltaIndex, DeltaIndexDao>(_mapperProvider);
        }
    }

    public class Sync
    {
        private readonly int _rangeSize;
        private readonly IUserOutput _userOutput;
        private readonly ILedger _ledger;
        private readonly IDeltaDfsReader _deltaDfsReader;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IPeerClientObservable _deltaHistoryResponseObserver;
        private readonly IMapperProvider _mapperProvider;

        private readonly IPeerSyncManager _peerSyncManager;
        private readonly IDeltaHeightWatcher _deltaHeightWatcher;

        private IDisposable _deltaHistorySubscription;

        public int CurrentDeltaIndex { private set; get; }
        public bool IsSynchronized { private set; get; }

        public Sync(IPeerSyncManager peerSyncManager,
            IDeltaHeightWatcher deltaHeightWatcher,
            ILedger ledger,
            IDeltaDfsReader deltaDfsReader,
            IDeltaIndexService deltaIndexService,
            IPeerClientObservable deltaHistoryResponseObserver,
            IMapperProvider mapperProvider,
            IUserOutput userOutput,
            int rangeSize = 100,
            int checkpoint = 10000)
        {
            _peerSyncManager = peerSyncManager;
            _deltaHeightWatcher = deltaHeightWatcher;

            _rangeSize = rangeSize;
            _ledger = ledger;
            _deltaDfsReader = deltaDfsReader;
            _deltaIndexService = deltaIndexService;
            _deltaHistoryResponseObserver = deltaHistoryResponseObserver;
            _mapperProvider = mapperProvider;
            _userOutput = userOutput;
        }

        public async Task StartAsync()
        {
            _userOutput.WriteLine("Starting Sync...");
            await _deltaHeightWatcher.StartAsync();

            _deltaHistorySubscription = _deltaHistoryResponseObserver.MessageStream.Subscribe(DeltaHistoryOnNext);

            await ProgressAsync();
        }

        public async Task StopAsync()
        {
            _userOutput.WriteLine("Stopping Sync...");
            await _deltaHeightWatcher.StopAsync();

            _deltaHistorySubscription.Dispose();
        }

        private async Task ProgressAsync()
        {
            CurrentDeltaIndex = _deltaIndexService.Height();

            await _deltaHeightWatcher.WaitForDeltaHeightAsync(CurrentDeltaIndex);

            var deltaHistoryRequest = new DeltaHistoryRequest
                {Height = (uint) CurrentDeltaIndex, Range = (uint) _rangeSize};
            _peerSyncManager.SendToRandomPeers(deltaHistoryRequest);
        }

        private void DeltaHistoryOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            if (!(peerClientMessageDto.Message is DeltaHistoryResponse deltaHistoryResponse))
            {
                return;
            }

            var deltaIndexes = deltaHistoryResponse.Result
               .Select(x => x.ToDao<DeltaIndex, DeltaIndexDao>(_mapperProvider)).OrderBy(x => x.Height).ToArray();

            UpdateIndexes(deltaIndexes);
            DownloadDeltas(deltaIndexes);
            UpdateState(deltaIndexes);

            ProgressAsync().GetAwaiter().GetResult();
        }

        private void UpdateIndexes(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            _deltaIndexService.Add(deltaIndexes);
            if (_deltaIndexService.Height() >= _deltaHeightWatcher.LatestDeltaHash.Height)
            {
                IsSynchronized = true;
            }
        }

        private void DownloadDeltas(IEnumerable<DeltaIndexDao> deltaIndexes)
        {
            foreach (var deltaIndex in deltaIndexes)
            {
                if (!_deltaDfsReader.TryReadDeltaFromDfs(deltaIndex.Cid, out _))
                {
                    throw new Exception($"Delta: {deltaIndex.Cid} not found in Dfs at height: {deltaIndex.Height}");
                }
            }
        }

        private void UpdateState(IEnumerable<DeltaIndexDao> deltaIndexes) { }
    }
}
