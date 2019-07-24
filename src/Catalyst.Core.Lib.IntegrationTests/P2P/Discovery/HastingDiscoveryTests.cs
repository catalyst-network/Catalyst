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
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Messaging.Correlation;
using Catalyst.Core.Lib.P2P.IO.Messaging.Dto;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Core.Lib.UnitTests.P2P.Discovery;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Core.Lib.IntegrationTests.P2P.Discovery
{
    public sealed class HastingDiscoveryTests
    {
        private readonly IPeerSettings _settings;
        private readonly IPeerIdentifier _ownNode;

        public HastingDiscoveryTests()
        {
            _settings = PeerSettingsHelper.TestPeerSettings();
            _ownNode = PeerIdentifierHelper.GetPeerIdentifier("ownNode");
        }

        [Fact]
        public async Task Evicted_Known_Ping_Message_Sets_Contacted_Neighbour_As_UnReachable_And_Can_RollBack_State()
        {
            var cacheEntriesByRequest = new Dictionary<ByteString, ICacheEntry>();

            var seedState = DiscoveryHelper.MockSeedState(_ownNode, _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.RestoreMemento(seedState);
            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            stateHistory = DiscoveryHelper.MockMementoHistory(stateHistory, 5);

            stateHistory.ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = DiscoveryHelper.MockPnr();
            var stateCandidate = DiscoveryHelper.MockOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();

            var memoryCache = Substitute.For<IMemoryCache>();
            
            var peerMessageCorrelationManager = DiscoveryHelper.MockCorrelationManager(default, memoryCache);
                
            var correlatableMessages = new List<CorrelatableMessage<ProtocolMessage>>();
            stateCandidate.UnResponsivePeers.ToList().ForEach(i =>
            {
                var (key, value) = i;
                cacheEntriesByRequest = CacheHelper.MockCacheEvictionCallback(value.Id.ToByteString(), memoryCache, cacheEntriesByRequest);

                var msg = new CorrelatableMessage<ProtocolMessage>
                {
                    Content = new PingRequest().ToProtocolMessage(_ownNode.PeerId, value),
                    Recipient = key
                };
                correlatableMessages.Add(msg);
                peerMessageCorrelationManager.AddPendingRequest(msg);
            });
            
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(PingResponseObserver))
               .WithPeerMessageCorrelationManager(peerMessageCorrelationManager)
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker(stateCareTaker)
               .WithCurrentState(seedOrigin)
               .WithStateCandidate(stateCandidate);
            
            using (var walker = discoveryTestBuilder.Build())
            {
                stateCandidate.UnResponsivePeers.ToList().ForEach(action: p =>
                {
                    cacheEntriesByRequest[p.Value.Id.ToByteString()]
                       .PostEvictionCallbacks[0]
                       .EvictionCallback
                       .Invoke(
                            p.Key,
                            (CorrelatableMessage<ProtocolMessage>) correlatableMessages.FirstOrDefault(i => i.Recipient.PeerId.Equals(p.Key.PeerId)),
                            EvictionReason.Expired,
                            new object()
                        );
                });
                
                walker.StateCandidate.UnResponsivePeers.Count.Should().Be(5);

                walker.HasValidCandidate().Should().BeFalse();

                var expectedCurrentState = walker.HastingCareTaker.HastingMementoList.Peek();
                
                walker.WalkBack();

                walker.State.Peer.Should().Be(expectedCurrentState.Peer);
                walker.State.CurrentPeersNeighbours.Count.Should().Be(0);
                walker.State.UnResponsivePeers.Count.Should().Be(5);
            }
        }
        
        [Fact]
        public async Task Expected_Ping_Response_From_All_Contacted_Nodes_Produces_Valid_State_Candidate()
        {
            var seedState = DiscoveryHelper.SubSeedState(_ownNode, _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.RestoreMemento(seedState);
            
            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            DiscoveryHelper.MockMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = DiscoveryHelper.MockPnr();
            var stateCandidate = DiscoveryHelper.SubOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();
        
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(PingResponseObserver))
               .WithPeerMessageCorrelationManager()
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker(stateCareTaker)
               .WithCurrentState(seedOrigin)
               .WithStateCandidate(stateCandidate);
            
            using (var walker = discoveryTestBuilder.Build())
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
                    
                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(dto);
                        });
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
            var seedState = DiscoveryHelper.SubSeedState(_ownNode, _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.RestoreMemento(seedState);
        
            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            DiscoveryHelper.MockMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = DiscoveryHelper.MockPnr();
            var stateCandidate = DiscoveryHelper.MockOriginator(default, default, knownPnr);
            stateCandidate.CurrentPeersNeighbours.Clear();
        
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(PingResponseObserver))
               .WithPeerMessageCorrelationManager()
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker(stateCareTaker)
               .WithCurrentState(seedOrigin)
               .WithStateCandidate(stateCandidate);
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();
        
                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var pingDto = new PeerClientMessageDto(new PingResponse(), 
                        stateCandidate.UnResponsivePeers.FirstOrDefault().Key,
                        stateCandidate.UnResponsivePeers.FirstOrDefault().Value
                    );
                    
                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(pingDto);
                        });
                    
                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();
        
                    streamObserver.Received(1).OnNext(Arg.Is(pingDto));
        
                    walker.StateCandidate.CurrentPeersNeighbours.Contains(pingDto.Sender);
                }
            }
        }
    }
}
