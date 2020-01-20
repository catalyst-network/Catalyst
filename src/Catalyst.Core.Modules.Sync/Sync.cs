using System;
using System.Linq;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.IPPN;

namespace Catalyst.Core.Modules.Sync
{
    public class Sync
    {
        private readonly uint _rangeSize;
        private int _deltaHeight;
        private uint _currentDeltaHeight;
        private readonly IPeerSettings _peerSettings;
        private readonly ILedger _ledger;
        private readonly IDeltaCache _deltaCache;
        private readonly IPeerClient _peerClient;
        private readonly IPeerRepository _peerRepository;
        private readonly DeltaHeightResponseObserver _deltaHeightResponseObserver;
        private readonly DeltaHistoryResponseObserver _deltaHistoryResponseObserver;

        private IDisposable _deltaHeightSubscription;
        private IDisposable _deltaHistorySubscription;

        public Sync(IPeerSettings peerSettings,
            ILedger ledger,
            IDeltaCache deltaCache,
            IPeerClient peerClient,
            IPeerRepository peerRepository,
            DeltaHeightResponseObserver deltaHeightResponseObserver,
            DeltaHistoryResponseObserver deltaHistoryResponseObserver,
            uint rangeSize = 20)
        {
            _rangeSize = rangeSize;
            _peerSettings = peerSettings;
            _ledger = ledger;
            _deltaCache = deltaCache;
            _peerClient = peerClient;
            _peerRepository = peerRepository;
            _deltaHeightResponseObserver = deltaHeightResponseObserver;
            _deltaHistoryResponseObserver = deltaHistoryResponseObserver;
        }

        public void SetDeltaCurrentIndex(uint currentDeltaHeight) { _currentDeltaHeight = currentDeltaHeight; }

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
            var peers = _peerRepository.GetActivePeers(10).First();
            var deltaHistoryRequest = new DeltaHistoryRequest {Height = _currentDeltaHeight, Range = _rangeSize};
            var protocolMessage = deltaHistoryRequest.ToProtocolMessage(_peerSettings.PeerId);

            _peerClient.SendMessage(new MessageDto(
                protocolMessage,
                peers.PeerId)
            );
        }

        private void GetDeltaHeight()
        {
            var peers = _peerRepository.GetActivePeers(10).First();
            var deltaHeightRequest = new LatestDeltaHashRequest();
            var protocolMessage = deltaHeightRequest.ToProtocolMessage(_peerSettings.PeerId);

            _peerClient.SendMessage(new MessageDto(
                protocolMessage,
                peers.PeerId)
            );
        }

        private void DeltaHistoryOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            var deltaHistoryResponse = peerClientMessageDto.Message as DeltaHistoryResponse;

            foreach (var deltaIndex in deltaHistoryResponse.Result)
            {
                _ledger.Update(deltaIndex.Cid.ToByteArray().ToCid());
            }

            Progress();
        }

        private void DeltaHeightOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            var latestDeltaHashResponse = peerClientMessageDto.Message as LatestDeltaHashResponse;
        }
    }
}
