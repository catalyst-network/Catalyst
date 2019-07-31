using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Repository;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Catalyst.Core.Lib.P2P
{
    public class PeerHeartbeatChecker : IPeerHeartbeatChecker
    {
        private readonly TimeSpan _checkHeartbeatInterval;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerChallenger _peerChallenger;
        private IDisposable _subscription;

        public PeerHeartbeatChecker(IPeerRepository peerRepository, IPeerChallenger peerChallenger, TimeSpan checkHeartbeatInterval)
        {
            _peerRepository = peerRepository;
            _peerChallenger = peerChallenger;
            _checkHeartbeatInterval = checkHeartbeatInterval;
        }

        public void Run()
        {
            _subscription = Observable
               .Interval(_checkHeartbeatInterval)
               .SubscribeOn(TaskPoolScheduler.Default)
               .StartWith(-1L)
               .Subscribe(interval => CheckHeartbeat());
        }

        private void CheckHeartbeat()
        {
            foreach (var peer in _peerRepository.GetAll())
            {
                Task.Run(async () =>
                {
                    var result = await _peerChallenger.ChallengePeerAsync(peer.PeerIdentifier).ConfigureAwait(false);
                    if (!result)
                    {
                        _peerRepository.Delete(peer.DocumentId);
                    }
                }).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
