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
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.Discovery
{
    public sealed class HastingsDiscovery 
        : IHastingsDiscovery, IDisposable
    {
        public readonly IDns Dns;
        private int _awaitedResponses;
        private int _unresponsivePeers;
        public readonly ILogger Logger;
        public IHastingsOriginator state;
        private int _discoveredPeerInCurrentWalk;
        public readonly IPeerClient _peerClient;
        private readonly IDtoFactory _dtoFactory;
        private readonly IPeerIdentifier _ownNode;
        private readonly int _peerDiscoveryBurnIn;
        private IHastingsOriginator _stateCandidate;
        private HastingCareTaker _hastingCareTaker;
        private readonly IRepository<Peer> _peerRepository;
        public readonly IDictionary<ICorrelationId, IPeerIdentifier> _cache;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        private readonly IDisposable _evictionSubscription;

        public IObservable<IPeerClientMessageDto> DiscoveryStream { get; private set; }

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
            new Dictionary<ICorrelationId, IPeerIdentifier>(),
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
            IDictionary<ICorrelationId, IPeerIdentifier> cache,
            int peerDiscoveryBurnIn = 10)
        
        {
            Dns = dns;
            _cache = cache;
            Logger = logger;
            _peerClient = peerClient;
            _dtoFactory = dtoFactory;
            var peerSettings1 = peerSettings;
            _peerRepository = peerRepository;
            _discoveredPeerInCurrentWalk = 0;
            _peerDiscoveryBurnIn = peerDiscoveryBurnIn;
            _hastingCareTaker = new HastingCareTaker();
            _cancellationTokenProvider = cancellationTokenProvider;
            _ownNode = new PeerIdentifier(peerSettings1, new PeerIdClientId("AC")); // this needs to be changed
            
            DiscoveryStream = Observable.Empty<IPeerClientMessageDto>();

            peerClientObservables.ToList()
               .GroupBy(p => p.MessageStream)
               .Select(p => p.Key)
               .ToList()
               .ForEach(s => DiscoveryStream = DiscoveryStream.Merge(s));

            DiscoveryStream
               .Where(i => i.Message.Descriptor.ShortenedFullName()
                   .Equals(PingResponse.Descriptor.ShortenedFullName())
                )
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(OnPingResponse, OnError, OnCompleted);
            
            DiscoveryStream
               .Where(i => i.Message.Descriptor.ShortenedFullName()
                   .Equals(PeerNeighborsResponse.Descriptor.ShortenedFullName())
                )
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(OnPeerNeighbourResponse, OnError, OnCompleted);
            
            // build the initial state of walk, which our node and seed nodes
            state = new HastingsOriginator
            {
                Peer = _ownNode,
                CurrentPeersNeighbours = new ConcurrentBag<IPeerIdentifier>(
                    Dns.GetSeedNodesFromDns(peerSettings1.SeedServers)
                       .ToList()
                )
            };
            
            // store state with caretaker
            _hastingCareTaker.Add(state.CreateMemento());

            _evictionSubscription = peerMessageCorrelationManager.EvictionEventStream.Subscribe(EvictionCallback);

            Task.Run(async () =>
            {
                await DiscoveryAsync();
            });
        }

        private void EvictionCallback(KeyValuePair<ICorrelationId, IPeerIdentifier> item)
        {
            if (item.Value.Equals(state.Peer))
            {
                // reset walk as did not recieve pnr
            }
            else if (_cache.ContainsKey(item.Key))
            {
                Interlocked.Increment(ref _unresponsivePeers);
            }
        }
        
        public async Task DiscoveryAsync()
        {
            do
            {
                try
                {
                    _stateCandidate = new HastingsOriginator
                    {
                        Peer = state.CurrentPeersNeighbours.RandomElement()
                    };
                    
                    var peerNeighbourRequestDto = _dtoFactory.GetDto(new PeerNeighborsRequest(),
                        _ownNode,
                        _stateCandidate.Peer
                    );

                    _cache.Add(peerNeighbourRequestDto.CorrelationId, _stateCandidate.Peer);
                    _peerClient.SendMessage(peerNeighbourRequestDto);

                    await WaitUntil(() => (_unresponsivePeers += _stateCandidate.CurrentPeersNeighbours.ToList().Count).Equals(
                        _awaitedResponses));
                    
                    _stateCandidate.CurrentPeersNeighbours.ToList().ForEach(StorePeer);

                    var nextState = _stateCandidate.CreateMemento();
                    _hastingCareTaker.Add(nextState);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            } while (!_cancellationTokenProvider.HasTokenCancelled());
        }
      
        private void OnPingResponse(IPeerClientMessageDto obj)
        {
            Logger.Debug("OnPingResponse");

            if (!_cache.TryGetValue(obj.CorrelationId, out IPeerIdentifier neighbour))
            {
                return;
            }

            _cache.Remove(obj.CorrelationId);
            _stateCandidate.CurrentPeersNeighbours.Add(neighbour);
            
            Interlocked.Decrement(ref _awaitedResponses);
        }

        private void OnPeerNeighbourResponse(IPeerClientMessageDto obj)
        {
            Logger.Debug("OnPeerNeighbourResponse");

            if (!_cache.TryGetValue(obj.CorrelationId, out _))
            {
                return;
            }

            _cache.Remove(obj.CorrelationId);

            var peerNeighbours = (PeerNeighborsResponse) obj.Message;

            _unresponsivePeers = 0;
                
            peerNeighbours.Peers.ToList().ForEach(p =>
            {
                var pingRequestDto = _dtoFactory.GetDto(new PingRequest(),
                    _ownNode,
                    _stateCandidate.Peer
                );

                _cache.Add(pingRequestDto.CorrelationId, pingRequestDto.RecipientPeerIdentifier);
                
                _peerClient.SendMessage(pingRequestDto);
            });

            _awaitedResponses = 0; // reset counter for new loop

            Interlocked.Add(ref _awaitedResponses, peerNeighbours.Peers.Count);
        }

        private void OnError(Exception obj)
        {
            throw new NotImplementedException();
        }

        private void OnCompleted()
        {
            throw new NotImplementedException();
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
                LastSeen = DateTime.UtcNow,
                PeerIdentifier = peerIdentifier,
                Reputation = 0
            });
                
            Interlocked.Add(ref _discoveredPeerInCurrentWalk, 1);
        }
        
        public void Dispose()
        {
            _peerClient?.Dispose();
            _peerRepository?.Dispose();
            _evictionSubscription?.Dispose();
        }
    }
}
