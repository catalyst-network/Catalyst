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
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using SharpRepository.Repository.Caching;
using Xunit;

namespace Catalyst.Core.Lib.IntegrationTests.P2P.Discovery
{
    public sealed class HastingDiscoveryTests
    {
        private IPeerSettings _settings;
        private ILogger _logger;
        private IPeerIdentifier _ownNode;

        public HastingDiscoveryTests()
        {
            _settings = PeerSettingsHelper.TestPeerSettings();
            _logger = Substitute.For<ILogger>();
            _ownNode = PeerIdentifierHelper.GetPeerIdentifier("ownNode");
        }
        
        /// <summary>
        ///  clean up
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cache"></param>
        private void AddCreateEntryExpectation(object key, IMemoryCache cache)
        {
            var correlationId = (ByteString) key;
            var cacheEntry = Substitute.For<ICacheEntry>();
            var expirationTokens = new List<IChangeToken>();
            cacheEntry.ExpirationTokens.Returns(expirationTokens);
            var expirationCallbacks = new List<PostEvictionCallbackRegistration>();
            cacheEntry.PostEvictionCallbacks.Returns(expirationCallbacks);

            cache.CreateEntry(correlationId).Returns(cacheEntry);
            CacheEntriesByRequest.Add(correlationId, cacheEntry);
        }

        protected readonly Dictionary<ByteString, ICacheEntry> CacheEntriesByRequest 
            = new Dictionary<ByteString, ICacheEntry>();

        [Fact]
        public async Task Evicted_Known_Ping_Message_Sets_Contacted_Neighbour_As_UnReachable()
        {
            var pr = new PingResponseObserver(Substitute.For<ILogger>());
            var peerClientObservers = new List<IPeerClientObservable> {pr};

            var seedState = HastingDiscoveryHelper.GenerateSeedState(_ownNode, _settings.SeedServers.ToList(), _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.SetMemento(seedState);
            
            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            HastingDiscoveryHelper.GenerateMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = HastingDiscoveryHelper.MockPnr();
            var stateCandidate = HastingDiscoveryHelper.MockOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();

            var memoryCache = Substitute.For<IMemoryCache>();
            
            var peerMessageCorrelationManager = new PeerMessageCorrelationManager(
                Substitute.For<IReputationManager>(),
                memoryCache,
                Substitute.For<ILogger>(),
                new TtlChangeTokenProvider(3));
                
            stateCandidate.ContactedNeighbours.ToList().ForEach(i =>
            {
                AddCreateEntryExpectation(i.Key.Id.ToByteString(), memoryCache);

                peerMessageCorrelationManager.AddPendingRequest(
                    new CorrelatableMessage<ProtocolMessage>
                    {
                        Content = new PingRequest().ToProtocolMessage(_ownNode.PeerId, i.Key),
                        Recipient = i.Value
                    });

                var x = CacheEntriesByRequest[i.Key.Id.ToByteString()].PostEvictionCallbacks.Count;
            });
            
            using (var walker = new HastingsDiscovery(
                Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_settings.SeedServers.ToList(), _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                peerMessageCorrelationManager,
                Substitute.For<ICancellationTokenProvider>(),
                peerClientObservers,
                false,
                0,
                seedOrigin,
                stateCareTaker,
                stateCandidate))
            {
                CacheEntriesByRequest.ToList().ForEach(i => i.Value.PostEvictionCallbacks.FirstOrDefault().EvictionCallback.Invoke());
                CacheEntriesByRequest[stateCandidate.ContactedNeighbours[0].Key.Id.ToByteString()].PostEvictionCallbacks[0].EvictionCallback
                   .Invoke();
            }
        }

        [Fact]
        public async Task Expected_Ping_Response_From_All_Contacted_Nodes_Produces_Valid_State_Candidate()
        {
            var pr = new PingResponseObserver(Substitute.For<ILogger>());
            var peerClientObservers = new List<IPeerClientObservable> {pr};

            var seedState = HastingDiscoveryHelper.GenerateSeedState(_ownNode, _settings.SeedServers.ToList(), _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.SetMemento(seedState);
            
            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            HastingDiscoveryHelper.GenerateMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = HastingDiscoveryHelper.MockPnr();
            var stateCandidate = HastingDiscoveryHelper.MockOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();

            using (var walker = new HastingsDiscovery(
                Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_settings.SeedServers.ToList(), _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                new PeerMessageCorrelationManager(
                    Substitute.For<IReputationManager>(),
                    Substitute.For<IMemoryCache>(),
                    Substitute.For<ILogger>(),
                    new TtlChangeTokenProvider(3)),
                Substitute.For<ICancellationTokenProvider>(),
                peerClientObservers,
                false,
                0,
                seedOrigin,
                stateCareTaker,
                stateCandidate))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                IList<IPeerClientMessageDto> dtoList = new List<IPeerClientMessageDto>();
                
                stateCandidate.ContactedNeighbours.ToList().ForEach(i =>
                {
                    var dto = new PeerClientMessageDto(new PingResponse(),
                        stateCandidate.ContactedNeighbours.FirstOrDefault().Value,
                        stateCandidate.ContactedNeighbours.FirstOrDefault().Key
                    );
                    
                    dtoList.Add(dto);
                    pr._responseMessageSubject.OnNext(dto);
                });
                
                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    walker.StateCandidate.CurrentPeersNeighbours
                       .Select(i => i.PeerId)
                       .Should()
                       .BeSubsetOf(
                            stateCandidate.ContactedNeighbours
                               .Select(i => i.Value.PeerId)
                        );
                }
            }
        }

        [Fact]
        public async Task Expected_Ping_Response_Sets_Neighbour_As_Reachable()
        {
            var pr = new PingResponseObserver(Substitute.For<ILogger>());
            var peerClientObservers = new List<IPeerClientObservable> {pr};

            var seedState = HastingDiscoveryHelper.GenerateSeedState(_ownNode, _settings.SeedServers.ToList(), _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.SetMemento(seedState);

            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            HastingDiscoveryHelper.GenerateMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = HastingDiscoveryHelper.MockPnr();
            var stateCandidate = HastingDiscoveryHelper.MockOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();

            using (var walker = new HastingsDiscovery(
                Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(), 
                HastingDiscoveryHelper.SubDnsClient(_settings.SeedServers.ToList(), _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                new PeerMessageCorrelationManager(
                    Substitute.For<IReputationManager>(),
                    Substitute.For<IMemoryCache>(),
                    Substitute.For<ILogger>(),
                    new TtlChangeTokenProvider(3)),
                Substitute.For<ICancellationTokenProvider>(),
                peerClientObservers,
                false,
                0,
                seedOrigin,
                stateCareTaker,
                stateCandidate))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var pingDto = new PeerClientMessageDto(new PingResponse(), 
                        stateCandidate.ContactedNeighbours.FirstOrDefault().Value,
                        stateCandidate.ContactedNeighbours.FirstOrDefault().Key
                    );
                    
                    pr._responseMessageSubject.OnNext(pingDto);
                    
                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();

                    streamObserver.Received(1).OnNext(Arg.Is(pingDto));

                    walker.StateCandidate.CurrentPeersNeighbours.Contains(pingDto.Sender);
                }
            }
        }
    }
}
