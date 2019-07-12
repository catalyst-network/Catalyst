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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Nethereum.KeyStore.Crypto;
using Nethereum.RLP;
using Serilog;
using SharpRepository.Repository;
using Tmds.Linux;

namespace Catalyst.Node.Core.P2P.Discovery
{
    public sealed class HastingsDiscovery 
        : IHastingsDiscovery, IDisposable
    {
        public IDns Dns { get; }
        public ILogger Logger { get; }
        public IRepository<Peer> PeerRepository { get; }
        public IObservable<IPeerClientMessageDto> DiscoveryStream { get; set; }
        
        private IPeerIdentifier _ownNode;
        public IHastingsOriginator state;
        private readonly IPeerClient _peerClient;
        private readonly IDtoFactory _dtoFactory;
        private HastingCareTaker _hastingCareTaker;
        private readonly IPeerSettings _peerSettings;
        protected readonly IMemoryCache StepCandidates;
        private Func<MemoryCacheEntryOptions> _cacheOptions;
        private readonly ICancellationTokenProvider _cancellationTokenProvider;
        private IDisposable discoverySubscription;
        
        public List<IPeerIdentifier> _x { get; set; }

        public HastingsDiscovery(ILogger logger,
            IRepository<Peer> peerRepository,
            IDns dns,
            IPeerSettings peerSettings,
            IPeerClient peerClient,
            IDtoFactory dtoFactory,
            ICancellationTokenProvider cancellationTokenProvider,
            IChangeTokenProvider changeTokenProvider)
        {
            Dns = dns;
            Logger = logger;
            PeerRepository = peerRepository;

            _peerClient = peerClient;
            _dtoFactory = dtoFactory;
            _peerSettings = peerSettings;
            _hastingCareTaker = new HastingCareTaker();
            _cancellationTokenProvider = cancellationTokenProvider;
            _ownNode = new PeerIdentifier(_peerSettings, new PeerIdClientId("AC")); // this needs to be changed

            DiscoveryStream = Observable.Empty<IPeerClientMessageDto>();
            
            _x = new List<IPeerIdentifier>();
            
            // _cacheOptions = () => new MemoryCacheEntryOptions()
            //    .AddExpirationToken(changeTokenProvider.GetChangeToken())
            //    .RegisterPostEvictionCallback(EvictionCallback);
            
            // build the initial state of walk, which our node and seed nodes
            state = BuildState(_ownNode, GetSeedNodes());
            
            // store state with caretaker
            _hastingCareTaker.Add(state.CreateMemento());
            
            DiscoveryStream.Where(m => _x.Contains(m.Sender))
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(OnNext, OnError, OnCompleted);
            
            Task.Run(async () =>
            {
                await DiscoveryAsync();
            });
        }

        private void OnError(Exception obj)
        {
            throw new NotImplementedException();
        }

        private void OnCompleted() { throw new NotImplementedException(); }

        private void OnNext(IPeerClientMessageDto obj)
        {
            Logger.Debug(obj.Sender.ToString());
        }

        public async Task DiscoveryAsync()
        {
            do
            {
                state.CurrentPeersNeighbours.ToList().ForEach(n =>
                {
                    var peerNeighbourRequestDto = BuildDtoMessage(n);
                    
                    StepCandidates.Set(peerNeighbourRequestDto.CorrelationId, n, _cacheOptions());
                    _peerClient.SendMessage(peerNeighbourRequestDto);
                });

                // peerNeighbourRequestDto.CorrelationId
            
                // _peerClient.SendMessage(peerNeighbourRequestDto);
            } while (!_cancellationTokenProvider.HasTokenCancelled());
        }

        private IHastingsOriginator BuildState(IPeerIdentifier currentNeighbour, IEnumerable<IPeerIdentifier> currentNeighboursPeers)
        {
            return new HastingsOriginator
            {
                Peer = currentNeighbour,
                CurrentPeersNeighbours = new ConcurrentBag<IPeerIdentifier>(currentNeighboursPeers)
            };
        }
        
        /// <summary>
        ///     Takes discovery messages from IPeerClientObservable and merges them here.
        /// </summary>
        /// <param name="reputationChangeStream"></param>
        public void MergeDiscoveryMessageStreams(IObservable<IPeerClientMessageDto> reputationChangeStream)
        {
            DiscoveryStream = DiscoveryStream.Merge(reputationChangeStream);
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state) { }

        private IMessageDto<PeerNeighborsRequest> BuildDtoMessage(IPeerIdentifier recipient)
        {
            return _dtoFactory.GetDto(new PeerNeighborsRequest(),
                _ownNode,
                recipient
            );
        }
        
        private IEnumerable<IPeerIdentifier> GetSeedNodes()
        {
            return Dns.GetSeedNodesFromDns(_peerSettings.SeedServers).ToList();
        }
        
        public void Dispose()
        {
            _peerClient?.Dispose();
            PeerRepository?.Dispose();
        }
    }
}
