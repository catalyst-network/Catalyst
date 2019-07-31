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

        public PeerHeartbeatChecker(IPeerRepository peerRepository, IPeerChallenger peerChallenger, int checkHeartbeatIntervalSeconds)
        {
            _peerRepository = peerRepository;
            _peerChallenger = peerChallenger;
            _checkHeartbeatInterval = TimeSpan.FromSeconds(checkHeartbeatIntervalSeconds);
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
