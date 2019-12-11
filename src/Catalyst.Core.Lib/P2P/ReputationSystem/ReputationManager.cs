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
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Catalyst.Abstractions.P2P.ReputationSystem;
using Catalyst.Core.Lib.P2P.Repository;
using Dawn;
using Serilog;

namespace Catalyst.Core.Lib.P2P.ReputationSystem
{
    public sealed class ReputationManager : IReputationManager, IDisposable
    {
        private readonly ILogger _logger;
        public IPeerRepository PeerRepository { get; }
        public readonly ReplaySubject<IPeerReputationChange> ReputationEvent;
        public IObservable<IPeerReputationChange> ReputationEventStream => ReputationEvent.AsObservable();
        public IObservable<IPeerReputationChange> MergedEventStream { get; set; }
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1);

        public ReputationManager(IPeerRepository peerRepository, ILogger logger, IScheduler scheduler = null)
        {
            var observableScheduler = scheduler ?? Scheduler.Default;

            _logger = logger;
            PeerRepository = peerRepository;
            ReputationEvent = new ReplaySubject<IPeerReputationChange>(0, observableScheduler);
            ReputationEventStream.Subscribe(OnNext, OnError, OnCompleted);
        }

        /// <summary>
        ///     Allows passing a reputation streams to merge with the MasterReputationEventStream
        /// </summary>
        /// <param name="reputationChangeStream"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void MergeReputationStream(IObservable<IPeerReputationChange> reputationChangeStream)
        {
            MergedEventStream = ReputationEventStream.Merge(reputationChangeStream);
        }

        private void OnCompleted() { _logger.Debug("Message stream ended."); }

        private void OnError(Exception exc) { _logger.Error("ReputationManager error: {@Exc}", exc); }

        // ReSharper disable once VSTHRD100
        public async void OnNext(IPeerReputationChange peerReputationChange)
        {
            try
            {
                await SemaphoreSlim.WaitAsync().ConfigureAwait(false);

                var peer = PeerRepository.GetAll().FirstOrDefault(p => p.PeerId.Equals(peerReputationChange.PeerId));
                Guard.Argument(peer, nameof(peer)).NotNull();

                // ReSharper disable once PossibleNullReferenceException
                peer.Reputation += peerReputationChange.ReputationEvent.Amount;
                PeerRepository.Update(peer);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public void Dispose()
        {
            ReputationEvent?.Dispose();
            PeerRepository?.Dispose();
        }
    }
}
