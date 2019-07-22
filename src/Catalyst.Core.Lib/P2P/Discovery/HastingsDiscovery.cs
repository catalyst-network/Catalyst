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
using Catalyst.Protocol;
using Catalyst.Protocol.IPPN;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Core.Lib.P2P.Discovery
{
    public class HastingsDiscovery 
        : IHastingsDiscovery, IDisposable
    {
        public readonly IDns Dns;
        public readonly ILogger Logger;
        public readonly IHastingsOriginator State;
        private int _discoveredPeerInCurrentWalk;
        public readonly IPeerClient PeerClient;
        public readonly IDtoFactory DtoFactory;
        private readonly IPeerIdentifier _ownNode;
        private readonly int _peerDiscoveryBurnIn;
        public readonly IHastingsOriginator StateCandidate;
        public readonly IHastingCareTaker HastingCareTaker;
        public readonly IRepository<Peer> PeerRepository;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        private readonly IDisposable _evictionSubscription;

        public IObservable<IPeerClientMessageDto> DiscoveryStream { get; private set; }

        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        internal HastingsDiscovery(ILogger logger = default,
            IRepository<Peer> peerRepository = default,
            IDns dns = default,
            IPeerSettings peerSettings = default,
            IPeerClient peerClient = default,
            IDtoFactory dtoFactory = default,
            IPeerMessageCorrelationManager peerMessageCorrelationManager = default,
            ICancellationTokenProvider cancellationTokenProvider = default,
            IEnumerable<IPeerClientObservable> peerClientObservables = default) : this(logger,
            peerRepository,
            dns,
            peerSettings,
            peerClient,
            dtoFactory,
            peerMessageCorrelationManager,
            cancellationTokenProvider,
            peerClientObservables,
            false, 
            0,
            default,
            default,
            null) { }
        
        public HastingsDiscovery(ILogger logger,
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
            Dns = dns;
            Logger = logger;
            PeerClient = peerClient;
            DtoFactory = dtoFactory;
            PeerRepository = peerRepository;
            _discoveredPeerInCurrentWalk = 0;
            _peerDiscoveryBurnIn = peerDiscoveryBurnIn;
            _cancellationTokenProvider = cancellationTokenProvider;
            HastingCareTaker = hastingCareTaker ?? new HastingCareTaker();
            _ownNode = new PeerIdentifier(peerSettings, new PeerIdClientId("AC")); // this needs to be changed
            
            // build the initial stateCandidate for walk,
            // which is our node and seed nodes
            StateCandidate = stateCandidate != null
                ? stateCandidate
                : new HastingsOriginator
                {
                    Peer = _ownNode,
                    CurrentPeersNeighbours =
                        new List<IPeerIdentifier>(Dns.GetSeedNodesFromDns(peerSettings.SeedServers))
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

            if (autoStart == false)
            {
                return;
            }
            
            WalkForward();
                
            // should run until cancelled
            Task.Run(async () =>
            {
                await DiscoveryAsync();
            });
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
        public async Task DiscoveryAsync()
        {
            do
            {
                // only let one thread in at a time.
                await SemaphoreSlim.WaitAsync();

                try
                {
                    // spins until our expected result equals found and unreachable peers for this step.
                    await WaitUntil(HasValidCandidate, 1000, -1);

                    lock (StateCandidate)
                    {
                        if (StateCandidate.CurrentPeersNeighbours.Count > 0)
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
                catch (Exception e) // either an exception was thrown for un-known reason or the await on the condition timed out.
                {
                    Logger.Error(e, e.Message);
                    lock (StateCandidate)
                    {
                        if (StateCandidate.CurrentPeersNeighbours.Count > 0)
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
                if (StateCandidate.CurrentPeersNeighbours.Count < 1 || StateCandidate.UnResponsivePeers.Count == 5)
                {
                    // if we haven't contacted neighbours don't try compare.
                    return false;
                }

                // see if sum of unreachable peers and reachable peers equals the total contacted number.
                return StateCandidate.UnResponsivePeers.Count
                   .Equals(StateCandidate.UnResponsivePeers.Count + StateCandidate.CurrentPeersNeighbours.Count);
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
                // store discovered peers.
                StateCandidate.CurrentPeersNeighbours
                   .ToList()
                   .ForEach(StorePeer);

                // create memento of state.
                var newState = StateCandidate.CreateMemento();

                // store state with caretaker.
                HastingCareTaker.Add(newState);
            
                // transition to valid new state.
                State.RestoreMemento(newState);

                // continue walk by proposing next degree.
                StateCandidate.Peer = State.CurrentPeersNeighbours.RandomElement();
            
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
                StateCandidate.Peer = lastState.Neighbours.RandomElement();
                
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
        private void EvictionCallback(KeyValuePair<ICorrelationId, IPeerIdentifier> item)
        {
            lock (StateCandidate)
            {
                if (StateCandidate.ExpectedPnr.Equals(item))
                {
                    // state candidate didn't give any neighbours so go back a step.
                    WalkBack();
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
            Logger.Debug("OnPingResponse");
            
            lock (StateCandidate)
            {
                if (!StateCandidate.UnResponsivePeers.Contains(new KeyValuePair<IPeerIdentifier, ICorrelationId>(obj.Sender, obj.CorrelationId)))
                {
                    // a pingResponse that isn't known to discovery.
                    Logger.Debug("UnKnownMessage");
                    return;
                }

                StateCandidate.CurrentPeersNeighbours.Add(obj.Sender);
            }
        }

        private void OnPeerNeighbourResponse(IPeerClientMessageDto obj)
        {
            Logger.Debug("OnPeerNeighbourResponse");
            
            lock (StateCandidate)
            {
                if (!StateCandidate.ExpectedPnr.Equals(new KeyValuePair<ICorrelationId, IPeerIdentifier>(obj.CorrelationId, obj.Sender)))
                {
                    // we shouldn't get here as we should always know about a pnr as only this class produces them.
                    throw new ArgumentException();
                }

                var peerNeighbours = (PeerNeighborsResponse) obj.Message;
            
                // state candidate provided us a list of neighbours, so now check they are reachable.
                peerNeighbours.Peers.ToList().ForEach(p =>
                {
                    var pingRequestDto = DtoFactory.GetDto(new PingRequest(),
                        _ownNode,
                        new PeerIdentifier(p)
                    );

                    var (key, value) = new KeyValuePair<ICorrelationId, IPeerIdentifier>(
                        pingRequestDto.CorrelationId,
                        pingRequestDto.RecipientPeerIdentifier);
                    
                    PeerClient.SendMessage(pingRequestDto);
                    
                    // our total expected responses should be same as number of pings sent out,
                    // potential neighbours, can either send response, or we will see them evicted from cache.
                    StateCandidate.UnResponsivePeers[value] = key;
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
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask,
                Task.Delay(timeout)))
            {
                throw new TimeoutException();
            }
        }

        /// <summary>
        ///     Stores a peer in the database unless we are in the burn-in phase.
        /// </summary>
        /// <param name="peerIdentifier"></param>
        /// <returns></returns>
        private void StorePeer(IPeerIdentifier peerIdentifier)
        {
            if (_discoveredPeerInCurrentWalk < _peerDiscoveryBurnIn)
            {
                // if where not past our burn in phase just continue.
                return;
            }
            
            PeerRepository.Add(new Peer
            {
                Reputation = 0,
                LastSeen = DateTime.UtcNow,
                PeerIdentifier = peerIdentifier
            });
                
            Interlocked.Add(ref _discoveredPeerInCurrentWalk, 1);
        }
        
        public void Dispose()
        {
            PeerClient?.Dispose();
            PeerRepository?.Dispose();
            _evictionSubscription?.Dispose();
        }
    }
}
