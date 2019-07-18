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
using FluentAssertions.Common;
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

        [Fact]
        public async Task Evicted_Known_Ping_Message_Sets_Contacted_Neighbour_As_UnReachable_And_Can_RollBack_State()
        {
            var cacheEntriesByRequest = new Dictionary<ByteString, ICacheEntry>();
            var pr = new PingResponseObserver(Substitute.For<ILogger>());
            var peerClientObservers = new List<IPeerClientObservable> {pr};

            var seedState = HastingDiscoveryHelper.MockSeedState(_ownNode, _settings.SeedServers.ToList(), _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.RestoreMemento(seedState);
            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            HastingDiscoveryHelper.MockMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = HastingDiscoveryHelper.MockPnr();
            var stateCandidate = HastingDiscoveryHelper.MockOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();

            var memoryCache = Substitute.For<IMemoryCache>();
            
            var peerMessageCorrelationManager = HastingDiscoveryHelper.MockCorrelationManager(default, memoryCache);
                
            var correlatableMessages = new List<CorrelatableMessage<ProtocolMessage>>();
            stateCandidate.UnResponsivePeers.ToList().ForEach(i =>
            {
                cacheEntriesByRequest = CacheHelper.MockCacheEvictionCallback(i.Value.Id.ToByteString(), memoryCache, cacheEntriesByRequest);

                var msg = new CorrelatableMessage<ProtocolMessage>
                {
                    Content = new PingRequest().ToProtocolMessage(_ownNode.PeerId, i.Value),
                    Recipient = i.Key
                };
                correlatableMessages.Add(msg);
                peerMessageCorrelationManager.AddPendingRequest(msg);
            });
            
            using (var walker = HastingDiscoveryHelper.GetTestInstanceOfDiscovery(
                Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.MockDnsClient(_settings, _settings.SeedServers.ToList()),
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
                stateCandidate.UnResponsivePeers.ToList().ForEach(p =>
                {
                    var (key, _) = p;
                    cacheEntriesByRequest[key.PeerId.ToByteString()]
                       .PostEvictionCallbacks[0]
                       .EvictionCallback
                       .Invoke(
                            key,
                            correlatableMessages.Where(i => i.Recipient.Equals(key)),
                            EvictionReason.Expired,
                            new object()
                        );
                });
                
                // for (var i = 0; i < cacheEntriesByRequest.Count; i++)
                // {
                //     cacheEntriesByRequest[stateCandidate.UnResponsivePeers[i].Value.Id.ToByteString()]
                //        .PostEvictionCallbacks[0]
                //        .EvictionCallback
                //        .Invoke(
                //             stateCandidate.UnResponsivePeers[i].Value, correlatableMessages[i], EvictionReason.Expired, new object()
                //         );   
                // }

                walker.StateCandidate.UnResponsivePeers.Count.Should().Be(5);

                walker.HasValidCandidate().Should().BeFalse();

                var expectedCurrentState = walker.HastingCareTaker.HastingMementoList.Peek();
                
                walker.WalkBack();

                walker.State.Peer.Should().Be(expectedCurrentState.Peer);
                walker.State.UnResponsivePeers.Count.Should().Be(0);
                walker.State.CurrentPeersNeighbours.Count.Should().Be(0);
                walker.State.UnResponsivePeers.Count.Should().Be(0);
            }
        }
        
        [Fact]
        public async Task Expected_Ping_Response_From_All_Contacted_Nodes_Produces_Valid_State_Candidate()
        {
            var pr = new PingResponseObserver(Substitute.For<ILogger>());
            var peerClientObservers = new List<IPeerClientObservable> {pr};

            var seedState = HastingDiscoveryHelper.SubSeedState(_ownNode, _settings.SeedServers.ToList(), _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.RestoreMemento(seedState);
            
            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            HastingDiscoveryHelper.MockMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = HastingDiscoveryHelper.MockPnr();
            var stateCandidate = HastingDiscoveryHelper.SubOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();

            using (var walker = HastingDiscoveryHelper.GetTestInstanceOfDiscovery(
                Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.MockDnsClient(_settings, _settings.SeedServers.ToList()),
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
                
                stateCandidate.UnResponsivePeers.ToList().ForEach(i =>
                {
                    var dto = new PeerClientMessageDto(new PingResponse(),
                        stateCandidate.UnResponsivePeers.FirstOrDefault().Key,
                        stateCandidate.UnResponsivePeers.FirstOrDefault().Value
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
                            stateCandidate.UnResponsivePeers
                               .Select(i => i.Key.PeerId)
                        );
                }
            }
        }

        [Fact]
        public async Task Expected_Ping_Response_Sets_Neighbour_As_Reachable()
        {
            var pr = new PingResponseObserver(Substitute.For<ILogger>());
            var peerClientObservers = new List<IPeerClientObservable> {pr};

            var seedState = HastingDiscoveryHelper.SubSeedState(_ownNode, _settings.SeedServers.ToList(), _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.RestoreMemento(seedState);

            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            HastingDiscoveryHelper.MockMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = HastingDiscoveryHelper.MockPnr();
            var stateCandidate = HastingDiscoveryHelper.MockOriginator(default, default, knownPnr);
            stateCandidate.CurrentPeersNeighbours.Clear();

            using (var walker = HastingDiscoveryHelper.GetTestInstanceOfDiscovery(
                Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.MockDnsClient(_settings, _settings.SeedServers.ToList()),
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
                        stateCandidate.UnResponsivePeers.FirstOrDefault().Key,
                        stateCandidate.UnResponsivePeers.FirstOrDefault().Value
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
