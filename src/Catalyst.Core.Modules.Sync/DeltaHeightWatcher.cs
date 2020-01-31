using System;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Modules.Sync.Interface;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;

namespace Catalyst.Core.Modules.Sync
{
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _deltaHeightSubscription = _deltaHeightResponseObserver.MessageStream.Subscribe(DeltaHeightOnNext);
        }

        public async Task StopAsync(CancellationToken cancellationToken) { _deltaHeightSubscription.Dispose(); }

        public async Task WaitForDeltaHeightAsync(int currentDeltaIndex)
        {
            while (LatestDeltaHash == null || LatestDeltaHash.Height == currentDeltaIndex)
            {
                _peerSyncManager.GetDeltaHeight();
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
}
