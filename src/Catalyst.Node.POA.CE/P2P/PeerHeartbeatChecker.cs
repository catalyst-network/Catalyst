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
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.P2P.Repository;
using Serilog;

namespace Catalyst.Node.POA.CE.P2P
{
    public sealed class PeerHeartbeatChecker : IPeerHeartbeatChecker
    {
        private readonly TimeSpan _checkHeartbeatInterval;
        private readonly IPeerRepository _peerRepository;
        private readonly IPeerChallenger _peerChallenger;
        private readonly int _maxNonResponsiveCounter;
        private readonly ConcurrentDictionary<string, int> _nonResponsivePeerMap;
        private readonly ILogger _logger;
        private IDisposable _subscription;

        public PeerHeartbeatChecker(ILogger logger,
            IPeerRepository peerRepository,
            IPeerChallenger peerChallenger,
            int checkHeartbeatIntervalSeconds,
            int maxNonResponsiveCounter)
        {
            _logger = logger;
            _nonResponsivePeerMap = new ConcurrentDictionary<string, int>();
            _peerRepository = peerRepository;
            _maxNonResponsiveCounter = maxNonResponsiveCounter;
            _peerChallenger = peerChallenger;
            _checkHeartbeatInterval = TimeSpan.FromSeconds(checkHeartbeatIntervalSeconds);
        }

        public void Run()
        {
            _subscription = Observable
               .Interval(_checkHeartbeatInterval)
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
                    var counterValue = _nonResponsivePeerMap.GetOrAdd(peer.DocumentId, 0);
                    _logger.Verbose(
                        $"Heartbeat result: {result} Peer: {peer.PeerIdentifier} Non-Responsive Counter: {counterValue}");
                    if (!result)
                    {
                        _nonResponsivePeerMap[peer.DocumentId] += 1;
                        counterValue += 1;

                        if (counterValue >= _maxNonResponsiveCounter)
                        {
                            _peerRepository.Delete(peer.DocumentId);
                            _nonResponsivePeerMap.TryRemove(peer.DocumentId, out _);
                            _logger.Verbose(
                                $"Peer reached maximum non-responsive count: {peer.PeerIdentifier}. Evicted from repository");
                        }
                    }
                    else
                    {
                        _nonResponsivePeerMap[peer.DocumentId] = 0;
                    }
                }).ConfigureAwait(false);
            }
        }

        void IDisposable.Dispose() { _subscription?.Dispose(); }
    }
}
