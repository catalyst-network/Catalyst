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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Network;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.P2P.Models;
using Catalyst.Protocol;
using Catalyst.Protocol.IPPN;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.P2P.Discovery.Hastings
{
    public class HastingsDiscovery
        : IHastingsDiscovery, IDisposable
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        private readonly IDisposable _evictionSubscription;
        private readonly int _hasValidCandidatesCheckMillisecondsFrequency;
        private readonly ILogger _logger;
        private readonly int _millisecondsTimeout;
        private readonly IDisposable _neigbourResponseSubscription;
        private readonly IPeerIdentifier _ownNode;
        private readonly IDisposable _pingResponseSubscriptions;
        protected readonly int PeerDiscoveryBurnIn;
        public readonly IRepository<Peer> PeerRepository;
        private int _discoveredPeerInCurrentWalk;

        protected bool IsDiscovering;

        protected HastingsDiscovery(ILogger logger,
            IRepository<Peer> peerRepository,
            IDns dns,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            IPeerMessageCorrelationManager peerMessageCorrelationManager,
            ICancellationTokenProvider cancellationTokenProvider,
            IEnumerable<IPeerClientObservable> peerClientObservables,
            bool autoStart = true,
            int peerDiscoveryBurnIn = 10,
            IHastingsOriginator stepProposal = default,
            IHastingsCareTaker hastingsCareTaker = default,
            int millisecondsTimeout = 10_000,
            int hasValidCandidatesCheckMillisecondsFrequency = 1_000)
        {
            _logger = logger;
            _cancellationTokenProvider = cancellationTokenProvider;

            PeerClient = peerClient;
            PeerRepository = peerRepository;
            PeerDiscoveryBurnIn = peerDiscoveryBurnIn;
            _millisecondsTimeout = millisecondsTimeout;
            _hasValidCandidatesCheckMillisecondsFrequency = hasValidCandidatesCheckMillisecondsFrequency;

            _discoveredPeerInCurrentWalk = 0;

            // build the initial step proposal for the walk, which is our node and seed nodes
            _ownNode = new PeerIdentifier(peerSettings); // this needs to be changed

            var neighbours = dns.GetSeedNodesFromDns(peerSettings.SeedServers).ToNeighbours();

            HastingsCareTaker = hastingsCareTaker ?? new HastingsCareTaker();
            if (HastingsCareTaker.HastingMementoList.IsEmpty)
            {
                var rootMemento = stepProposal?.CreateMemento() ?? new HastingsMemento(_ownNode, neighbours);
                HastingsCareTaker.Add(rootMemento);
            }

            StepProposal = stepProposal ?? new HastingsOriginator(CurrentStep);

            // create an empty stream for discovery messages
            DiscoveryStream = Observable.Empty<IPeerClientMessageDto>();

            // merge the streams of all our IPeerClientObservable on to our empty DiscoveryStream.
            peerClientObservables
               .GroupBy(p => p.MessageStream)
               .Select(p => p.Key)
               .ToList()
               .ForEach(s => DiscoveryStream = DiscoveryStream.Merge(s));

            // register subscription for ping response messages.
            _pingResponseSubscriptions = DiscoveryStream
               .Where(i => i.Message.Descriptor.ShortenedFullName()
                   .Equals(PingResponse.Descriptor.ShortenedFullName())
                )
               .Subscribe(OnPingResponse);

            // register subscription from peerNeighbourResponse.
            _neigbourResponseSubscription = DiscoveryStream
               .Where(i => i.Message.Descriptor.ShortenedFullName()
                   .Equals(PeerNeighborsResponse.Descriptor.ShortenedFullName())
                )
               .Subscribe(OnPeerNeighbourResponse);

            // subscribe to evicted peer messages, so we can cross
            // reference them with discovery messages we sent
            _evictionSubscription = peerMessageCorrelationManager
               .EvictionEventStream
               .Select(e =>
                {
                    _logger.Debug("Eviction stream receiving {key}", e.Key);
                    return e.Key;
                })
               .Subscribe(EvictionCallback);

            if (!autoStart)
            {
                return;
            }

            WalkForward();

            // should run until cancelled
            Task.Run(async () =>
            {
                await DiscoveryAsync()
                   .ConfigureAwait(false);
            });
        }

        public IHastingsMemento CurrentStep => HastingsCareTaker.Peek();
        public IHastingsOriginator StepProposal { get; }

        public IHastingsCareTaker HastingsCareTaker { get; }

        public IPeerClient PeerClient { get; }
        public IObservable<IPeerClientMessageDto> DiscoveryStream { get; private set; }

        public void Dispose() { Dispose(true); }

        /// <summary>
        ///     Discovery mechanism for Hasting metropolis walk.
        ///     method loops until we can build up a valid next state,
        ///     a valid next state is when we see a sum of events on our Discovery stream that
        ///     equal to the number of peers we tried to discover. The discovery streams is merged from
        ///     PeerCorrelation cache where we listen for evicted pingResponses (the ones we sen to try discover the peer),
        ///     and PingResponses. Expected PingResponse messages indicate a potential neighbour has responded to our ping,
        ///     so we assume it is reachable, so we add this to the StepProposal.CurrentNeighbours.
        ///     Once the sum of StepProposal.CurrentNeighbours and EvictedPingResponses equals the total expected responses,
        ///     At this point we received all the potential messages we could, if we have potential peers for the next step, then
        ///     we can walk forward.
        ///     if not the condition has not been met within a timeout then we walk back by taking the last known state from the
        ///     IHastingCaretaker.
        /// </summary>
        /// <returns></returns>
        public async Task DiscoveryAsync()
        {
            if (IsDiscovering)
            {
                return;
            }

            do
            {
                // only let one thread in at a time.
                await SemaphoreSlim.WaitAsync().ConfigureAwait(false);

                IsDiscovering = true;

                try
                {
                    // spins until our expected result equals found and unreachable peers for this step.
                    await WaitUntil(StepProposal.HasValidCandidate, _hasValidCandidatesCheckMillisecondsFrequency,
                        _millisecondsTimeout).ConfigureAwait(false);

                    //lock (StepProposal)
                    {
                        if (StepProposal.Neighbours.Any())
                        {
                            // have a walkable next step, so continue walk.
                            WalkForward();
                        }
                        else
                        {
                            // we've received enough matching events but no StepProposal.CurrentPeersNeighbours
                            // Assume all neighbours provided are un-reachable.
                            WalkBack();
                        }
                    }
                }
                catch (Exception e
                ) // either an exception was thrown for un-known reason or the await on the condition timed out.
                {
                    _logger.Error(e, e.Message);
                    if (StepProposal.Neighbours.Any())
                    {
                        // if we discovered at least some peers we can continue walk.
                        WalkForward();
                    }
                    else
                    {
                        // we got an exception and found 0 contactable peers, so go back to previous state.
                        WalkBack();
                    }
                }
                finally
                {
                    // step out lock block.
                    SemaphoreSlim.Release();
                }

                // discovery should run until canceled.
            } while (!_cancellationTokenProvider.HasTokenCancelled());
        }

        /// <summary>
        ///     Transitions StepProposal to state,
        ///     then start to try building next StepProposal.
        /// </summary>
        protected void WalkForward()
        {
            if (!StepProposal.HasValidCandidate())
            {
                return;
            }

            var responsiveNeighbours = StepProposal.Neighbours
               .Where(n => n.StateTypes == NeighbourStateTypes.Responsive)
               .ToList();

            // store discovered peers.
            responsiveNeighbours.ForEach(StorePeer);

            // create memento of state.
            var newState = StepProposal.CreateMemento();

            // store state with caretaker.
            HastingsCareTaker.Add(newState);

            // continue walk by proposing next degree.
            var newCandidate = CurrentStep.Neighbours
               .Where(n => n.StateTypes == NeighbourStateTypes.Responsive)
               .RandomElement().PeerIdentifier;

            StepProposal.RestoreMemento(new HastingsMemento(newCandidate, new Neighbours()));

            var peerNeighbourRequestDto = new MessageDto(new PeerNeighborsRequest().ToProtocolMessage(_ownNode.PeerId),
                StepProposal.Peer
            );

            PeerClient.SendMessage(peerNeighbourRequestDto);
        }

        /// <summary>
        ///     Transition back to last state.
        /// </summary>
        protected void WalkBack()
        {
            var unresponsiveNeighbour = StepProposal.Peer;

            List<INeighbour> responsiveNeighbours;
            do
            {
                responsiveNeighbours = CurrentStep.Neighbours
                   .Where(n => !n.PeerIdentifier.Equals(unresponsiveNeighbour)
                     && n.StateTypes == NeighbourStateTypes.Responsive)
                   .ToList();

                if (!responsiveNeighbours.Any())
                {
                    //move back one step if the current step
                    //has no more potentially valid peers to suggest.
                    HastingsCareTaker.Get();
                }
            } while (!responsiveNeighbours.Any() && HastingsCareTaker.HastingMementoList.Count > 1);

            if (!responsiveNeighbours.Any())
            {
                throw new InvalidOperationException(
                    "Peer discovery walked failed reaching its starting point with no responsive neighbours.");
            }

            var newCandidate = responsiveNeighbours.RandomElement().PeerIdentifier;

            StepProposal.RestoreMemento(new HastingsMemento(newCandidate, new Neighbours()));

            var peerNeighbourRequestDto = new MessageDto(new PeerNeighborsRequest().ToProtocolMessage(_ownNode.PeerId, StepProposal.PnrCorrelationId),
                StepProposal.Peer
            );

            PeerClient.SendMessage(peerNeighbourRequestDto);
        }

        /// <summary>
        ///     OnNext method for _evictionSubscription
        ///     handles the discovery messages to see if there of interest to us.
        /// </summary
        /// /
        /// /
        /// /
        /// <param name="requestCorrelationId">Correlation Id for the request getting evicted.</param>
        protected void EvictionCallback(ICorrelationId requestCorrelationId)
        {
            _logger.Debug("Eviction callback called.");

            try
            {
                if (StepProposal.PnrCorrelationId.Equals(requestCorrelationId))
                {
                    // state candidate didn't give any neighbours so go back a step.
                    _logger.Verbose("StepProposal {n.PeerIdentifier} unresponsive.");
                    WalkBack();
                }

                var neighbour =
                    StepProposal.Neighbours.SingleOrDefault(n =>
                        n.DiscoveryPingCorrelationId.Equals(requestCorrelationId));
                if (neighbour == null)
                {
                    _logger.Debug(
                        "EvictionCallback received for {correlationId}, but not correlated to neighbours of {stateCandidate}",
                        requestCorrelationId, CurrentStep.Peer);
                    return;
                }

                _logger.Verbose("Neighbour {peerIdentifier} unresponsive.", neighbour.PeerIdentifier);
                neighbour.StateTypes = NeighbourStateTypes.UnResponsive;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to handle eviction callback.");
            }
        }

        /// <summary>
        ///     OnNext for PingResponse discovery messages that are of interest to us.
        ///     If it's not continue, if expecting this message it means a potential neighbour is reachable,
        ///     and should be added to the next state candidate.
        /// </summary>
        /// <param name="obj"></param>
        private void OnPingResponse(IPeerClientMessageDto obj)
        {
            _logger.Debug("OnPingResponse");
            try
            {
                if (!StepProposal.Neighbours
                   .Select(n => n.DiscoveryPingCorrelationId)
                   .ToList()
                   .Contains(obj.CorrelationId))
                {
                    // a pingResponse that isn't known to discovery.
                    _logger.Debug("UnKnownMessage");
                    return;
                }

                StepProposal.Neighbours.First(n => n.PeerIdentifier.Equals(obj.Sender)).StateTypes =
                    NeighbourStateTypes.Responsive;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to handle PingResponse");
            }
        }

        private void OnPeerNeighbourResponse(IPeerClientMessageDto obj)
        {
            _logger.Debug("OnPeerNeighbourResponse");

            try
            {
                if (!StepProposal.PnrCorrelationId.Equals(obj.CorrelationId))
                {
                    // we shouldn't get here as we should always know about a pnr as only this class produces them.
                    return;
                }

                var peerNeighbours = (PeerNeighborsResponse) obj.Message;

                if (peerNeighbours.Peers.Count.Equals(0))
                {
                    // we should think of deducting reputation in this situation.
                    return;
                }

                var newNeighbours = peerNeighbours.Peers.Select(p => new Neighbour(new PeerIdentifier(p))).ToList();

                // state candidate provided us a list of neighbours, so now check they are reachable.
                newNeighbours.ForEach(n =>
                {
                    try
                    {
                        var pingRequestDto = new MessageDto(new PingRequest().ToProtocolMessage(_ownNode.PeerId, n.DiscoveryPingCorrelationId),
                            n.PeerIdentifier);

                        PeerClient.SendMessage(pingRequestDto);

                        // our total expected responses should be same as number of pings sent out,
                        // potential neighbours, can either send response, or we will see them evicted from cache.
                        n.StateTypes = NeighbourStateTypes.Contacted;
                    }
                    catch (Exception e)
                    {
                        n.StateTypes = NeighbourStateTypes.UnResponsive;
                        _logger.Error(e, "Failed to send ping request to neighbour {neighbour}, marked as {state}", n,
                            n.StateTypes);
                    }
                });

                var newValidState = new HastingsMemento(StepProposal.Peer, new Neighbours(newNeighbours));

                StepProposal.RestoreMemento(newValidState);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to handle PeerNeighbourResponse");
            }
        }

        /// <summary>
        ///     Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        private static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition())
                {
                    await Task.Delay(frequency).ConfigureAwait(false);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)).ConfigureAwait(false))
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        ///     Stores a peer in the database unless we are in the burn-in phase.
        /// </summary>
        /// <param name="neighbour"></param>
        /// <returns></returns>
        protected void StorePeer(INeighbour neighbour)
        {
            if (_discoveredPeerInCurrentWalk < PeerDiscoveryBurnIn)
            {
                // if where not past our burn in phase just continue.
                Interlocked.Add(ref _discoveredPeerInCurrentWalk, 1);
                return;
            }

            PeerRepository.Add(new Peer
            {
                Reputation = 0,
                LastSeen = DateTime.UtcNow,
                PeerIdentifier = neighbour.PeerIdentifier
            });

            Interlocked.Add(ref _discoveredPeerInCurrentWalk, 1);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _evictionSubscription?.Dispose();
            _pingResponseSubscriptions?.Dispose();
            _neigbourResponseSubscription?.Dispose();
        }
    }
}
