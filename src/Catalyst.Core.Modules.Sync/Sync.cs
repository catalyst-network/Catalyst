using System;
using System.Linq;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;
using Lib.P2P;

namespace Catalyst.Core.Modules.Sync
{
    public class Sync
    {
        private readonly int _rangeSize;
        private Cid _latestDeltaHash;
        private readonly IPeerSettings _peerSettings;
        private readonly ILedger _ledger;
        private readonly IPeerClient _peerClient;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly IPeerRepository _peerRepository;
        private readonly DeltaHeightResponseObserver _deltaHeightResponseObserver;
        private readonly DeltaHistoryResponseObserver _deltaHistoryResponseObserver;
        private readonly IMapperProvider _mapperProvider;

        private IDisposable _deltaHeightSubscription;
        private IDisposable _deltaHistorySubscription;
        private int _currentDeltaIndex;

        public Sync(IPeerSettings peerSettings,
            ILedger ledger,
            IPeerClient peerClient,
            IDeltaIndexService deltaIndexService,
            IPeerRepository peerRepository,
            DeltaHeightResponseObserver deltaHeightResponseObserver,
            DeltaHistoryResponseObserver deltaHistoryResponseObserver,
            IMapperProvider mapperProvider,
            int rangeSize = 20)
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
            _currentDeltaIndex = _deltaIndexService.Height();

            var peers = _peerRepository.GetActivePeers(10).First();
            var deltaHistoryRequest = new DeltaHistoryRequest
                {Height = (uint) _currentDeltaIndex, Range = (uint) _rangeSize};
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
            var deltaHistoryResponse = (DeltaHistoryResponse) peerClientMessageDto.Message;
            var deltaIndexArray = deltaHistoryResponse.Result
               .Select(x => x.ToDao<DeltaIndex, DeltaIndexDao>(_mapperProvider)).OrderBy(x => x.Height);

            foreach (var deltaIndex in deltaIndexArray)
            {
                _ledger.Update(deltaIndex.Cid);
                _deltaIndexService.Add(deltaIndex);
                _currentDeltaIndex = deltaIndex.Height;

                if (_latestDeltaHash == deltaIndex.Cid) { }
            }

            Progress();
        }

        private void DeltaHeightOnNext(IPeerClientMessageDto peerClientMessageDto)
        {
            var latestDeltaHashResponse = (LatestDeltaHashResponse) peerClientMessageDto.Message;
            _latestDeltaHash = latestDeltaHashResponse.DeltaHash.ToByteArray().ToCid();
        }
    }
}
