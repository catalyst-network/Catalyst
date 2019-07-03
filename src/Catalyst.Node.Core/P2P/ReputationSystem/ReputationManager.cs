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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.P2P;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.ReputationSystem
{
    public class ReputationManager : IReputationManager, IDisposable
    {
        private readonly ILogger _logger;
        private IRepository<Peer> PeerRepository { get; }
        private readonly ReplaySubject<IPeerReputationChange> _reputationEvent;
        public IObservable<IPeerReputationChange> MasterReputationEventStream => _reputationEvent.AsObservable();

        public ReputationManager(IRepository<Peer> peerRepository, ILogger logger)
        {
            _logger = logger;
            PeerRepository = peerRepository;
            _reputationEvent = new ReplaySubject<IPeerReputationChange>(0);
            
            MasterReputationEventStream
               .SubscribeOn(NewThreadScheduler.Default)
               .Subscribe(OnNext, OnError, OnCompleted);
        }
        
        /// <summary>
        ///     Allows passing a reputation streams to merge with the MasterReputationEventStream
        /// </summary>
        /// <param name="reputationChangeStream"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void MergeReputationStream(IObservable<IPeerReputationChange> reputationChangeStream)
        {
            MasterReputationEventStream.Merge(reputationChangeStream);
        }

        private void OnCompleted()
        {
            _logger.Debug("Message stream ended.");
        }

        private void OnError(Exception obj)
        {
            _logger.Error("Message stream ended.");
        }

        private void OnNext(IPeerReputationChange peerReputationChange)
        {
            PeerRepository.TryFind(p => p.PeerIdentifier.Equals(peerReputationChange.PeerIdentifier), out var peer);
            peer.Reputation += peerReputationChange.ReputationEvent.Amount;
            PeerRepository.Update(peer);
        }
        
        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _reputationEvent?.Dispose();
            PeerRepository?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
