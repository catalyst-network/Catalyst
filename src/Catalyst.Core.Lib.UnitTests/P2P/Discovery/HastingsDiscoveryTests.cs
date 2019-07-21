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
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.P2P;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DnsClient;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Type = System.Type;

namespace Catalyst.Core.Lib.UnitTests.P2P.Discovery
{
    public sealed class HastingsDiscoveryTests
    {
        private readonly IPeerSettings _settings;
        private ILogger _logger;
        private readonly IPeerIdentifier _ownNode;

        public HastingsDiscoveryTests()
        {
            _settings = PeerSettingsHelper.TestPeerSettings();
            _logger = Substitute.For<ILogger>();
            _ownNode = PeerIdentifierHelper.GetPeerIdentifier("ownNode");
        }

        [Fact]
        public void Discovery_Can_Build_Initial_State_From_SeedNodes()
        {   
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithAutoStart(false)
               .WithBurn(0);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.State.Peer.PublicKey.ToHex()
                   .Equals("33326b7373683569666c676b336a666d636a7330336c646a346866677338676e");

                walker.StateCandidate.CurrentPeersNeighbours.Should().HaveCount(5);   
            }
        }

        [Theory]
        [InlineData(typeof(PingResponse), typeof(PingResponseObserver), "OnPingResponse")]
        [InlineData(typeof(PeerNeighborsResponse), typeof(GetNeighbourResponseObserver), "OnPeerNeighbourResponse")]
        public async Task Can_Merge_PeerClientObservable_Stream_And_Read_Items_Pushed_On_Separate_Streams(Type discoveryMessage, Type observer, string logMsg)
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithDns()
               .WithPeerClientObservables(default, observer)
               .WithCurrentState(DiscoveryHelper.SubSeedOriginator(_ownNode, _settings))
               .WithStateCandidate(default, false, default, default, DiscoveryHelper.MockPnr());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(discoveryMessage, walker.StateCandidate.ExpectedPnr.Key, walker.StateCandidate.ExpectedPnr.Value);

                    discoveryTestBuilder._peerClientObservables
                       .ToList()
                       .ForEach(o =>
                        {
                            o._responseMessageSubject.OnNext(subbedDto);
                        });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
                }
            }
        }
        
        [Fact]
        public async Task Can_Correlate_Discovery_Ping_And_Store_Active_Peer_In_Originator()
        {
            var expectedResponses = DiscoveryHelper.MockContactedNeighboursValuePairs();

            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(PingResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithStateCandidate(default, false, _ownNode, default, DiscoveryHelper.MockPnr(), expectedResponses);
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();
        
                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    expectedResponses.ToList().ForEach(r =>
                    {
                        discoveryTestBuilder._peerClientObservables.ToList()
                           .ForEach(o =>
                            {
                                o._responseMessageSubject.OnNext(DiscoveryHelper.SubDto(typeof(PingResponse), r.Value, r.Key));
                            });    
                    });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);
        
                    streamObserver
                       .Received(5)
                       .OnNext(Arg.Any<IPeerClientMessageDto>());

                    walker.StateCandidate.CurrentPeersNeighbours
                       .Should()
                       .NotContain(expectedResponses);
                    
                    walker.StateCandidate.UnResponsivePeers
                       .Should()
                       .Contain(expectedResponses);

                    walker.StateCandidate.UnResponsivePeers.Count.Should().Be(5);
                }
            }
        }

        [Fact]
        public async Task Can_Discard_UnKnown_PingResponse()
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(PingResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithStateCandidate(default, false, _ownNode, default, DiscoveryHelper.MockPnr());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto1 = Substitute.For<IPeerClientMessageDto>();
                    subbedDto1.Sender.Returns(Substitute.For<IPeerIdentifier>());
                    subbedDto1.CorrelationId.Returns(Substitute.For<ICorrelationId>());
                    subbedDto1.Message.Returns(new PingResponse());

                    discoveryTestBuilder._peerClientObservables.ToList().ForEach(o => o._responseMessageSubject.OnNext(subbedDto1));

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
                    walker.Logger.Received(1).Debug(Arg.Is("UnKnownMessage"));
                    
                    walker.StateCandidate.CurrentPeersNeighbours.Count.Should().Be(5);
                }
            }
        }

        [Fact]
        public void Can_Discard_UnKnown_PeerNeighbourResponse_Message()
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithStateCandidate(default, false, _ownNode, default, DiscoveryHelper.MockPnr());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(typeof(PingResponse));

                    discoveryTestBuilder._peerClientObservables
                       .ToList()
                       .ForEach(o =>
                        {
                            o._responseMessageSubject.OnNext(subbedDto);
                        });

                    walker.StateCandidate.CurrentPeersNeighbours
                       .Received(0)
                       .Add(Arg.Any<IPeerIdentifier>());
                }
            }
        }

        [Fact]
        public async Task Can_Process_Valid_PeerNeighbourResponse_Message_And_Ping_Provided_Neighbours()
        {
            var pnr = DiscoveryHelper.MockPnr(_ownNode);
            var expectedResponses = DiscoveryHelper.MockContactedNeighboursValuePairs();

            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithStateCandidate(default, false, _ownNode, default, pnr, new Dictionary<IPeerIdentifier, ICorrelationId>());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(typeof(PeerNeighborsResponse), pnr.Key, pnr.Value);
                    
                    var peerNeighborsResponse = new PeerNeighborsResponse();
                    peerNeighborsResponse.Peers.Add(expectedResponses.ToList().Select(kv => kv.Key.PeerId));
                   
                    subbedDto.Message.Returns(peerNeighborsResponse);
                    
                    discoveryTestBuilder._peerClientObservables.ToList()
                       .ForEach(o =>
                        {
                            o._responseMessageSubject.OnNext(subbedDto);
                        });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);

                    // walker.StateCandidate.UnResponsivePeers.Received(5).Add(Arg.Any<KeyValuePair<IPeerIdentifier, ICorrelationId>>());

                    walker.DtoFactory
                       .Received(5)
                       .GetDto(
                            Arg.Is(new PingRequest()),
                            Arg.Any<IPeerIdentifier>(),
                            Arg.Any<IPeerIdentifier>()
                        );
                    
                    walker.PeerClient
                       .Received(5)
                       .SendMessage(Arg.Any<IMessageDto<PingRequest>>());
                    
                    walker.StateCandidate.UnResponsivePeers.Count.Should().Be(5);
                }
            }
        }

        [Fact]
        public async Task Known_Evicted_Correlation_Cache_PingRequest_Message_Increments_UnResponsivePeer()
        {
            var pnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(),
                PeerIdentifierHelper.GetPeerIdentifier("sender")
            );
            
            var evictionEvent = new ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>>(0);

            evictionEvent.OnNext(pnr);
            
            var initialMemento = DiscoveryHelper.SubMemento(_ownNode, DiscoveryHelper.MockDnsClient(_settings)
               .GetSeedNodesFromDns(_settings.SeedServers).ToList()
            );
            
            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer = initialMemento.Peer;
            initialStateCandidate.CurrentPeersNeighbours.Returns(Substitute.For<IList<IPeerIdentifier>>());
            
            initialStateCandidate.CreateMemento().Returns(initialMemento);
            
            initialStateCandidate.UnResponsivePeers.Contains(
                Arg.Any<KeyValuePair<IPeerIdentifier, ICorrelationId>>()).Returns(true);

            initialStateCandidate.ExpectedPnr.ReturnsForAnyArgs(pnr);

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                DiscoveryHelper.MockDnsClient(_settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                DiscoveryHelper.SubCancellationProvider(),
                Substitute.For<IList<IPeerClientObservable>>(),
                false,
                0,
                initialStateCandidate,
                Substitute.For<IHastingCareTaker>(),
                initialStateCandidate
            ))
            {
                walker.StateCandidate.ReceivedWithAnyArgs(1).UnResponsivePeers.Count();
            }
        }

        [Fact]
        public void Known_Evicted_PeerNeighbourRequest_Walk_Backs_State()
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new GetNeighbourResponseObserver(_logger)
            };

            var pnr = DiscoveryHelper.MockPnr();

            var subbedCareTaker = Substitute.For<IHastingCareTaker>();
            
            var currentState = DiscoveryHelper.SubMemento();
            var currentOriginator = DiscoveryHelper.SubOriginator(currentState.Peer, currentState.Neighbours);

            var stateCandidate = DiscoveryHelper.SubMemento();
            var stateCandidateOriginator =
                DiscoveryHelper.SubOriginator(stateCandidate.Peer, DiscoveryHelper.SubSeedOriginator(_ownNode, _settings).CurrentPeersNeighbours);

            var mockedHistory = new Stack<IHastingMemento>();

            var seed = DiscoveryHelper.SubSeedState(_ownNode, _settings);
            mockedHistory.Push(seed);
            subbedCareTaker.HastingMementoList.Returns(mockedHistory);
            subbedCareTaker.Get().Returns(seed);
            
            var evictionEvent = new ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>>(0);
            evictionEvent.OnNext(pnr);
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                DiscoveryHelper.MockDnsClient(_settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                DiscoveryHelper.SubCancellationProvider(),
                peerClientObservers,
                false,
                0,
                currentOriginator,
                subbedCareTaker,
                stateCandidateOriginator
            ))
            {
                walker.State.Peer
                   .Should()
                   .Be(currentOriginator.Peer);
                
                walker.StateCandidate.Peer
                   .Should()
                   .Be(stateCandidateOriginator.Peer);

                walker.StateCandidate
                   .Received(1)
                   .ExpectedPnr.Equals(Arg.Is(pnr));
                
                // walker.HastingCareTaker.Received(1).Get();
                // walker.State.Received(1).RestoreMemento(Arg.Any<IHastingMemento>());
                // walker.DtoFactory.Received(1).GetDto(new PeerNeighborsRequest(), Arg.Is(_ownNode), Arg.Any<IPeerIdentifier>());

                // walker.PeerClient.ReceivedWithAnyArgs(1).SendMessage(Arg.Any<IMessageDto<PeerNeighborsRequest>>());
            }
        }
    }
}
