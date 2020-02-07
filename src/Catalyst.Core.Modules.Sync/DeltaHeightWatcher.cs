using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Ledger;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.IPPN;

namespace Catalyst.Core.Modules.Sync
{
    public class DeltaHeightWatcher : IDeltaHeightWatcher
    {
        public DeltaIndex LatestDeltaHash { private set; get; }
        private readonly IMapperProvider _mapperProvider;

        private readonly IPeerSyncManager _peerSyncManager;
        private IDisposable _deltaHeightSubscription;
        private readonly IPeerService _peerService;
        //private readonly IPeerClientObservable _deltaHeightResponseObserver;

        public DeltaHeightWatcher(IPeerSyncManager peerSyncManager,
            //IP2PMessageObserver deltaHeightResponseObserver,
            IPeerService peerService,
            IMapperProvider mapperProvider)
        {
            _peerSyncManager = peerSyncManager;
            _peerService = peerService;
            //_deltaHeightResponseObserver = (IPeerClientObservable) deltaHeightResponseObserver;
            _mapperProvider = mapperProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _peerService.MessageStream.Where(x => x.Payload != null &&
                    x.Payload.TypeUrl.EndsWith(typeof(LatestDeltaHashResponse).ShortenedProtoFullName()))
               .Select(x => x.Payload.FromProtocolMessage<LatestDeltaHashResponse>()).Subscribe(DeltaHeightOnNext);
            //_deltaHeightSubscription = _deltaHeightResponseObserver.MessageStream.Subscribe(DeltaHeightOnNext);
        }

        public async Task StopAsync(CancellationToken cancellationToken) { _deltaHeightSubscription.Dispose(); }

        public async Task WaitForDeltaHeightAsync(int currentDeltaIndex, CancellationToken cancellationToken)
        {
            while (LatestDeltaHash == null || LatestDeltaHash.Height == currentDeltaIndex)
            {
                _peerSyncManager.GetDeltaHeight();
                await Task.Delay(100, cancellationToken);
            }
        }

        //Use more peers for samples
        private void DeltaHeightOnNext(LatestDeltaHashResponse latestDeltaHashResponse)
        {
            LatestDeltaHash = 107;
            //LatestDeltaHash = latestDeltaHashResponse.Result;
        }

        public void Dispose() { }
    }
}
