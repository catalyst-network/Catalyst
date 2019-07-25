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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.P2P;
using Catalyst.Common.P2P.Discovery;
using Catalyst.Protocol;
using Catalyst.Protocol.IPPN;
using Microsoft.Azure.Documents.Client;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.P2P.Discovery
{
    public class HastingsDiscovery 
        : IHastingsDiscovery, IDisposable
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        protected bool IsDiscovering;
        private readonly ILogger _logger;
        public IHastingsOriginator State { get; }
        private int _discoveredPeerInCurrentWalk;
        private readonly IPeerIdentifier _ownNode;
        protected readonly int PeerDiscoveryBurnIn;
        public IHastingsOriginator StateCandidate { get; }
        public IHastingCareTaker HastingCareTaker { get; }
        public readonly IRepository<Peer> PeerRepository;
        private readonly IDisposable _evictionSubscription;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        
        public IPeerClient PeerClient { get; private set; }
        public IDtoFactory DtoFactory { get; private set; }
        public IObservable<IPeerClientMessageDto> DiscoveryStream { get; private set; }

        protected HastingsDiscovery(ILogger logger,
            IRepository<Peer> peerRepository,
            IDns dns,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            IDtoFactory dtoFactory,
            IPeerMessageCorrelationManager peerMessageCorrelationManager,
            ICancellationTokenProvider cancellationTokenProvider,
            IEnumerable<IPeerClientObservable> peerClientObservables,
            bool autoStart = true,
            int peerDiscoveryBurnIn = 10,
            IHastingsOriginator state = default,
            IHastingCareTaker hastingCareTaker = default,
            IHastingsOriginator stateCandidate = null)
        {
            _logger = logger;
            PeerClient = peerClient;
            DtoFactory = dtoFactory;
            PeerRepository = peerRepository;
            _discoveredPeerInCurrentWalk = 0;
            PeerDiscoveryBurnIn = peerDiscoveryBurnIn;
            _cancellationTokenProvider = cancellationTokenProvider;
            HastingCareTaker = hastingCareTaker ?? new HastingCareTaker();
            _ownNode = new PeerIdentifier(peerSettings, new PeerIdClientId("AC")); // this needs to be changed
            
            // build the initial stateCandidate for walk,
            // which is our node and seed nodes
            StateCandidate = stateCandidate ?? new HastingsOriginator
            {
                Peer = _ownNode,
                Neighbours = dns.GetSeedNodesFromDns(peerSettings.SeedServers).ToNeighbours().ToList()
            };

            // create an empty stream for discovery messages
            DiscoveryStream = Observable.Empty<IPeerClientMessageDto>();

            // merge the streams of all our IPeerClientObservable on to our empty DiscoveryStream.
            peerClientObservables.ToList()
               .GroupBy(p => p.MessageStream)
               .Select(p => p.Key)
               .ToList()
               .ForEach(s => DiscoveryStream = DiscoveryStream.Merge(s));

            // register subscription for ping response messages.
            DiscoveryStream
               .Where(i => i.Message.Descriptor.ShortenedFullName()
                   .Equals(PingResponse.Descriptor.ShortenedFullName())
                )
               .SubscribeOn(Scheduler.CurrentThread)
               .Subscribe(OnPingResponse);

            // register subscription from peerNeighbourResponse.
            DiscoveryStream
               .Where(i => i.Message.Descriptor.ShortenedFullName()
                   .Equals(PeerNeighborsResponse.Descriptor.ShortenedFullName())
                )
               .SubscribeOn(Scheduler.CurrentThread)
               .Subscribe(OnPeerNeighbourResponse);

            // subscribe to evicted peer messages, so we can cross
            // reference them with discovery messages we sent
            _evictionSubscription = peerMessageCorrelationManager
               .EvictionEventStream
               .SubscribeOn(Scheduler.CurrentThread)
               .Subscribe(EvictionCallback);

            // no state provide is assumed a "live run", instantiated with state assumes test run so don't start discovery
            State = state ?? new HastingsOriginator();

            if (autoStart)
            {
                WalkForward();

                // should run until cancelled
                Task.Run(async () =>
                {
                    await DiscoveryAsync(1000)
                       .ConfigureAwait(false);
                });
            }
        }

        /// <summary>
        ///     Discovery mechanism for Hasting metropolis walk.
        ///     method loops until we can build up a valid next state,
        ///     a valid next state is when we see a sum of events on our Discovery stream that
        ///     equal to the number of peers we tried to discover. The discovery streams is merged from
        ///     PeerCorrelation cache where we listen for evicted pingResponses (the ones we sen to try discover the peer),
        ///     and PingResponses. Expected PingResponse messages indicate a potential neighbour has responded to our ping,
        ///     so we assume it is reachable, so we add this to the StateCandidate.CurrentNeighbours.
        ///     Once the sum of StateCandidate.CurrentNeighbours and EvictedPingResponses equals the total expected responses,
        ///     At this point we received all the potential messages we could, if we have potential peers for the next step, then we can walk forward.
        ///     if not the condition has not been met within a timeout then we walk back by taking the last known state from the IHastingCaretaker.
        /// </summary>
        /// <returns></returns>
        public async Task DiscoveryAsync(int timeout = -1)
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
                    await WaitUntil(HasValidCandidate, 1000, timeout).ConfigureAwait(false);

                    lock (StateCandidate)
                    {
                        if (StateCandidate.Neighbours.Count > 0)
                        {
                            // have a walkable next step, so continue walk.
                            WalkForward();
                        }
                        else
                        {
                            // we've received enough matching events but no StateCandidate.CurrentPeersNeighbours
                            // Assume all neighbours provided are un-reachable.
                            WalkBack();
                        }
                    }
                }
                catch (Exception e
                ) // either an exception was thrown for un-known reason or the await on the condition timed out.
                {
                    _logger.Error(e, e.Message);
                    lock (StateCandidate)
                    {
                        if (StateCandidate.Neighbours.Count > 0)
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
                }
                finally
                {
                    // step out lock block.
                    SemaphoreSlim.Release();
                }

                // discovery should run until canceled.
            } while (!_cancellationTokenProvider.HasTokenCancelled());
        }

        protected bool HasValidCandidate()
        {
            lock (StateCandidate)
            {
                if (StateCandidate.Neighbours.ToList()
                   .Select(n => n.State)
                   .Count(i => i == Enumeration.Parse<NeighbourState>("NotContacted") || i == Enumeration.Parse<NeighbourState>("UnResponsive"))
                   .Equals(Constants.AngryPirate))
                {
                    return false;
                }

                // see if sum of unreachable peers and reachable peers equals the total contacted number.
                return StateCandidate.Neighbours
                   .ToList()
                   .Select(n => n.State)
                   .Count(s => s == Enumeration.Parse<NeighbourState>("UnResponsive") ||
                        s == Enumeration.Parse<NeighbourState>("Responsive"))
                   .Equals(Constants.AngryPirate);
            }
        }

        /// <summary>
        ///     Transitions StateCandidate to state,
        ///     then start to try building next StateCandidate.
        /// </summary>
        protected void WalkForward()
        {
            lock (State) 
            lock (StateCandidate)
            {
                if (!HasValidCandidate())
                {
                    return;
                }
                
                // store discovered peers.
                StateCandidate.Neighbours
                   .ToList()
                   .ForEach(StorePeer);

                // create memento of state.
                var newState = StateCandidate.CreateMemento();

                // store state with caretaker.
                HastingCareTaker.Add(newState);
            
                // transition to valid new state.
                State.RestoreMemento(newState);

                // continue walk by proposing next degree.
                StateCandidate.Peer = State.Neighbours.RandomElement().PeerIdentifier;
            
                var peerNeighbourRequestDto = DtoFactory.GetDto(new PeerNeighborsRequest(),
                    _ownNode,
                    StateCandidate.Peer
                );

                // make discovery wait for this pnr response.
                StateCandidate.ExpectedPnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(peerNeighbourRequestDto.CorrelationId, StateCandidate.Peer);
                    
                PeerClient.SendMessage(peerNeighbourRequestDto);                
            }
        }
        
        /// <summary>
        ///     Transition back to last state.
        /// </summary>
        protected void WalkBack()
        {
            lock (State)
            lock (StateCandidate)
            {
                var lastState = HastingCareTaker.Get();
             
                // continues walk by proposing a new degree.
                StateCandidate.Peer = lastState.Neighbours.Any()
                    ? lastState.Neighbours.RandomElement().PeerIdentifier
                    : throw new InvalidOperationException();

                // transitions to last state.
                State.RestoreMemento(lastState);

                var peerNeighbourRequestDto = DtoFactory.GetDto(new PeerNeighborsRequest(),
                    _ownNode,
                    StateCandidate.Peer
                );
                
                PeerClient.SendMessage(peerNeighbourRequestDto);   

                // make discovery wait for this pnr response
                StateCandidate.ExpectedPnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(peerNeighbourRequestDto.CorrelationId, StateCandidate.Peer);
            }
        }

        /// <summary>
        ///     OnNext method for _evictionSubscription
        ///     handles the discovery messages to see if there of interest to us.
        /// </summary>
        /// <param name="item"></param>
        protected void EvictionCallback(KeyValuePair<ICorrelationId, IPeerIdentifier> item)
        {
            lock (StateCandidate)
            {
                if (StateCandidate.ExpectedPnr.Equals(item))
                {
                    // state candidate didn't give any neighbours so go back a step.
                    WalkBack();
                }
                else
                {
                    StateCandidate.Neighbours
                       .First(n => item.Key
                               .Equals(n.DiscoveryPingCorrelationId)
                         && item.Value
                               .Equals(n.PeerIdentifier)
                        ).State = NeighbourState.UnResponsive;
                }
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
            
            lock (StateCandidate)
            {
                if (!StateCandidate.Neighbours
                   .Select(n => n.DiscoveryPingCorrelationId)
                   .ToList()
                   .Contains(obj.CorrelationId))
                {
                    // a pingResponse that isn't known to discovery.
                    _logger.Debug("UnKnownMessage");
                    return;
                }

                StateCandidate.Neighbours
                   .Add(
                        new Neighbour(obj.Sender)
                    );
            }
        }

        private void OnPeerNeighbourResponse(IPeerClientMessageDto obj)
        {
            _logger.Debug("OnPeerNeighbourResponse");
            
            lock (StateCandidate)
            {
                if (!StateCandidate.ExpectedPnr.Equals(new KeyValuePair<ICorrelationId, IPeerIdentifier>(obj.CorrelationId, obj.Sender)))
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
            
                // state candidate provided us a list of neighbours, so now check they are reachable.
                peerNeighbours.Peers.ToList().ForEach(p =>
                {
                    var pingRequestDto = DtoFactory.GetDto(new PingRequest(),
                        _ownNode,
                        new PeerIdentifier(p)
                    );

                    PeerClient.SendMessage(pingRequestDto);
                    
                    // our total expected responses should be same as number of pings sent out,
                    // potential neighbours, can either send response, or we will see them evicted from cache.
                    StateCandidate.Neighbours.First(n => n.PeerIdentifier.Equals(new PeerIdentifier(p))).State = NeighbourState.Contacted;
                });
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
        /// <param name="peerIdentifier"></param>
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
        
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            
            PeerClient?.Dispose();
            PeerRepository?.Dispose();
            _evictionSubscription?.Dispose();
        }
    }
}
