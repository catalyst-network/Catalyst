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
using System.Collections;
using System.Collections.Concurrent;
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
using Nito.Comparers.Linq;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Discovery
{
    public sealed class HastingsDiscovery 
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
        private readonly IHastingCareTaker _hastingCareTaker;
        private readonly IRepository<Peer> _peerRepository;
        public readonly IList<KeyValuePair<ICorrelationId, IPeerIdentifier>> Cache;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        private readonly IDisposable _evictionSubscription;

        public IObservable<IPeerClientMessageDto> DiscoveryStream { get; private set; }

        static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        public HastingsDiscovery(ILogger logger,
            IRepository<Peer> peerRepository,
            IDns dns,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            IDtoFactory dtoFactory,
            IPeerMessageCorrelationManager peerMessageCorrelationManager,
            ICancellationTokenProvider cancellationTokenProvider,
            IEnumerable<IPeerClientObservable> peerClientObservables,
            int peerDiscoveryBurnIn = 10) : this(logger,
            peerRepository,
            dns,
            peerSettings,
            peerClient,
            dtoFactory,
            peerMessageCorrelationManager,
            cancellationTokenProvider,
            peerClientObservables,
            new List<KeyValuePair<ICorrelationId, IPeerIdentifier>>(),
            peerDiscoveryBurnIn
        ) { }
            
        public HastingsDiscovery(ILogger logger,
            IRepository<Peer> peerRepository,
            IDns dns,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            IDtoFactory dtoFactory,
            IPeerMessageCorrelationManager peerMessageCorrelationManager,
            ICancellationTokenProvider cancellationTokenProvider,
            IEnumerable<IPeerClientObservable> peerClientObservables,
            IList<KeyValuePair<ICorrelationId, IPeerIdentifier>> cache,
            int peerDiscoveryBurnIn = 10,
            IHastingCareTaker hastingCareTaker = default)
        
        {
            Dns = dns;
            Cache = cache;
            Logger = logger;
            PeerClient = peerClient;
            DtoFactory = dtoFactory;
            var peerSettings1 = peerSettings;
            _peerRepository = peerRepository;
            _discoveredPeerInCurrentWalk = 0;
            _peerDiscoveryBurnIn = peerDiscoveryBurnIn;
            
            _hastingCareTaker = hastingCareTaker ?? new HastingCareTaker();

            _cancellationTokenProvider = cancellationTokenProvider;
            _ownNode = new PeerIdentifier(peerSettings1, new PeerIdClientId("AC")); // this needs to be changed
            
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

            // build the initial state of walk, which is our node and seed nodes
            StateCandidate = new HastingsOriginator
            {
                Peer = _ownNode,
                CurrentPeersNeighbours = new ConcurrentBag<IPeerIdentifier>(
                    Dns.GetSeedNodesFromDns(peerSettings1.SeedServers)
                       .ToList()
                )
            };
            
            WalkForward();

            // subscribe to evicted peer messages, so we can cross
            // reference them with discovery messages we sent
            _evictionSubscription = peerMessageCorrelationManager
               .EvictionEventStream
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(onNext: EvictionCallback);

            Task.Run(async () =>
            {
                await DiscoveryAsync();
            });
        }

        public async Task DiscoveryAsync()
        {
            do
            {
                await SemaphoreSlim.WaitAsync();

                try
                {
                    await WaitUntil(() => (
                            StateCandidate.UnreachableNeighbour + StateCandidate.CurrentPeersNeighbours.ToList().Count)
                       .Equals(State.ExpectedResponses)
                    );

                    WalkForward();
                }
                catch (Exception e)
                {
                    Logger.Error(e, e.Message);
                    WalkBack();
                }
                finally
                {
                    SemaphoreSlim.Release();
                }
            } while (!_cancellationTokenProvider.HasTokenCancelled());
        }

        private void WalkForward()
        {
            StateCandidate.CurrentPeersNeighbours
               .ToList()
               .ForEach(StorePeer);

            var newState = StateCandidate.CreateMemento();
            
            _hastingCareTaker.Add(newState);
            
            State.SetMemento(newState);

            StateCandidate.Peer = State.CurrentPeersNeighbours.RandomElement();
            
            var peerNeighbourRequestDto = DtoFactory.GetDto(new PeerNeighborsRequest(),
                _ownNode,
                StateCandidate.Peer
            );

            Cache.Add(new KeyValuePair<ICorrelationId, IPeerIdentifier>(peerNeighbourRequestDto.CorrelationId, StateCandidate.Peer));
                    
            PeerClient.SendMessage(peerNeighbourRequestDto);
        }
        
        private void WalkBack()
        {
            State.SetMemento(_hastingCareTaker.Get());
            StateCandidate.CurrentPeersNeighbours.Clear();
            StateCandidate.Peer = State.CurrentPeersNeighbours.RandomElement();
        }

        private void EvictionCallback(KeyValuePair<ICorrelationId, IPeerIdentifier> item)
        {
            if (StateCandidate.Peer.Equals(item.Value))
            {
                // state candidate give any neighbours so go back a step.
                WalkBack();
            }
            else if (Cache.Contains(new KeyValuePair<ICorrelationId, IPeerIdentifier>(item.Key, item.Value)))
            {
                // peer hasn't responded to ping so assume un-responsive.
                StateCandidate.IncrementUnreachablePeer();
                Cache.Remove(new KeyValuePair<ICorrelationId, IPeerIdentifier>(item.Key, item.Value));
            }
        }
      
        private void OnPingResponse(IPeerClientMessageDto obj)
        {
            Logger.Debug("OnPingResponse");
            
            if (!Cache.ToList().Contains(new KeyValuePair<ICorrelationId, IPeerIdentifier>(obj.CorrelationId, obj.Sender)))
            {
                return;
            }

            Cache.Remove(new KeyValuePair<ICorrelationId, IPeerIdentifier>(obj.CorrelationId, obj.Sender));
            StateCandidate.CurrentPeersNeighbours.Add(obj.Sender);
        }

        private void OnPeerNeighbourResponse(IPeerClientMessageDto obj)
        {
            Logger.Debug("OnPeerNeighbourResponse");

            var currentStepPnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(obj.CorrelationId, obj.Sender);
            
            if (!Cache.Contains(currentStepPnr))
            {
                return;
            }

            if (!Cache.Remove(currentStepPnr))
            {
                return;
            }

            var peerNeighbours = (PeerNeighborsResponse) obj.Message;
            
            peerNeighbours.Peers.ToList().ForEach(p =>
            {
                var pingRequestDto = DtoFactory.GetDto(new PingRequest(),
                    _ownNode,
                    StateCandidate.Peer
                );

                Cache.Add(new KeyValuePair<ICorrelationId, IPeerIdentifier>(
                    pingRequestDto.CorrelationId,
                    pingRequestDto.RecipientPeerIdentifier)
                );
                
                PeerClient.SendMessage(pingRequestDto);
            });

            StateCandidate.ExpectedResponses = peerNeighbours.Peers.Count;
        }

        /// <summary>
        ///     Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        private static async Task WaitUntil(Func<bool> condition, int frequency = 1000, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition())
                {
                    await Task.Delay(frequency);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
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
                return;
            }
            
            _peerRepository.Add(new Peer
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
            _peerRepository?.Dispose();
            _evictionSubscription?.Dispose();
        }
    }
}
