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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Xunit;
using Type = System.Type;

namespace Catalyst.Core.Lib.UnitTests.P2P.Discovery
{
    public sealed class HastingsDiscoveryTests
    {
        private readonly IPeerSettings _settings;
        private readonly IPeerIdentifier _ownNode;

        public HastingsDiscoveryTests()
        {
            _settings = PeerSettingsHelper.TestPeerSettings();
            _ownNode = PeerIdentifierHelper.GetPeerIdentifier("ownNode");
        }

        [Fact]
        public void Can_Store_Peer_After_Burn_In()
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStateCandidate()
               .WithCurrentState()
               .WithAutoStart()
               .WithBurn(5);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.GetBurnInValue()
                   .Should()
                   .Be(5);
                
                Enumerable.Range(0, 5).ToList().ForEach(i =>
                {
                    walker.TestStorePeer(Substitute.For<INeighbour>());
                
                    walker.PeerRepository
                       .Received(0)
                       .Add(Arg.Any<Peer>()); 
                });
                
                Enumerable.Range(0, 5).ToList().ForEach(i =>
                {
                    walker.TestStorePeer(Substitute.For<INeighbour>());
                });
                
                walker.PeerRepository
                   .Received(5)
                   .Add(Arg.Any<Peer>()); 
            }
        }

        [Fact]
        public void Cant_Store_Peer_During_Burn_In()
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStateCandidate()
               .WithCurrentState()
               .WithAutoStart()
               .WithBurn(10);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.GetBurnInValue()
                   .Should()
                   .Be(10);
                
                walker.TestStorePeer(Substitute.For<INeighbour>());
                
                walker.PeerRepository
                   .Received(0)
                   .Add(Arg.Any<Peer>());
            }
        }

        [Fact]
        public void Can_WalkForward_With_Valid_Candidate()
        {
            var knownStepPid =
                PeerIdentifierHelper.GetPeerIdentifier("hey_its_jimmys_brother_the_guy_with_the_beautiful_voice");

            var knownNextCandidate =
                PeerIdentifierHelper.GetPeerIdentifier("these_eyes....");
            
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStateCandidate(default,
                    true,
                    knownNextCandidate,
                    DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourState.Responsive))
               .WithCurrentState(default, true, knownStepPid)
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.State.Peer
                   .Should()
                   .Be(knownStepPid);
                
                walker.WalkForward();
                
                walker.State.Peer
                   .Should()
                   .Be(knownNextCandidate);
            }
        }

        [Fact]
        public void Can_Not_WalkForward_With_InValid_Candidate()
        {
            var knownStepPid =
                PeerIdentifierHelper.GetPeerIdentifier("hey_its_jimmys_brother_the_guy_with_the_beautiful_voice");

            var knownNextCandidate =
                PeerIdentifierHelper.GetPeerIdentifier("these_eyes....");
            
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStateCandidate(default,
                    true,
                    knownNextCandidate,
                    DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourState.UnResponsive))
               .WithCurrentState(default, true, knownStepPid)
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.State.Peer
                   .Should()
                   .Be(knownStepPid);
                
                walker.WalkForward();
                
                walker.State.Peer
                   .Should()
                   .Be(knownStepPid);
            }
        }

        [Fact]
        public void HasValidCandidate_Can_Validate_Correct_State()
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStateCandidate(default,
                    true,
                    default,
                    DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourState.Responsive))
               .WithCurrentState()
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.HasValidCandidate().Should().BeTrue();
            }
        }

        [Fact]
        public void HasValidCandidate_Can_Detect_Invalid_State()
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStateCandidate(default,
                    true,
                    default,
                    DiscoveryHelper.MockNeighbours(0))
               .WithCurrentState()
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.HasValidCandidate().Should().BeFalse();
            }
        }
        
        [Fact]
        public void Can_Throw_Exception_In_WalkBack_When_Last_State_Has_No_Neighbours_To_Continue_Walk_Forward()
        {
            var ctp = new CancellationTokenProvider();
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, false)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider(ctp)
               .WithPeerClientObservables()
               .WithStateCandidate()
               .WithCurrentState()
               .WithAutoStart(false)
               .WithBurn(0);

            using (var walker = discoveryTestBuilder.Build())
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    walker.DiscoveryAsync(2).GetAwaiter().GetResult();
                    Thread.Sleep(2);
                    ctp.CancellationTokenSource.Cancel();
                });
            }
        }

        [Fact]
        public void Can_Get_State_From_CareTaker()
        {
            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, false)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithAutoStart(false)
               .WithBurn(0);

            using (var walker = discoveryTestBuilder.Build())
            {
                var lastState = walker.HastingCareTaker.Get();
                lastState.Peer.Should().BeAssignableTo<IPeerIdentifier>();
            }
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

                walker.StateCandidate.Neighbours
                   .Should()
                   .HaveCount(Constants.AngryPirate); // http://giphygifs.s3.amazonaws.com/media/9MFsKQ8A6HCN2/giphy.gif
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
               .WithStateCandidate(default, false, default, default, CorrelationId.GenerateCorrelationId());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(discoveryMessage, walker.StateCandidate.PnrCorrelationId, walker.StateCandidate.Peer);

                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(subbedDto);
                        });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
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
               .WithStateCandidate(default, false, _ownNode, default, CorrelationId.GenerateCorrelationId());
            
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

                    discoveryTestBuilder.PeerClientObservables.ToList().ForEach(o => o.ResponseMessageSubject.OnNext(subbedDto1));

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
                    
                    walker.StateCandidate.Neighbours.Count.Should().Be(0);
                }
            }
        }

        [Fact]
        public void Unknown_Pnr_Message_Does_Not_Walk_Back()
        {
            var candidatePid = PeerIdentifierHelper.GetPeerIdentifier("candidate");
            var currentPid = PeerIdentifierHelper.GetPeerIdentifier("current");

            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithStateCandidate(default, false, candidatePid, default)
               .WithCurrentState(default, true, currentPid);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.TestEvictionCallback(CorrelationId.GenerateCorrelationId());

                walker.State.Peer
                   .Should()
                   .Be(currentPid);

                walker.StateCandidate.Peer
                   .Should()
                   .Be(candidatePid);
            }
        }
        
        [Fact]
        public void Known_Pnr_Message_Does_Walk_Back()
        {
            var candidatePid = PeerIdentifierHelper.GetPeerIdentifier("candidate");
            var currentPid = PeerIdentifierHelper.GetPeerIdentifier("current");
            var lastPid = PeerIdentifierHelper.GetPeerIdentifier("last");
            var previousState = DiscoveryHelper.SubMemento(lastPid);
            var history = new Stack<IHastingMemento>();
            history.Push(previousState);
            var knownPnr = CorrelationId.GenerateCorrelationId();

            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithStateCandidate(default, false, candidatePid, default, knownPnr)
               .WithCurrentState(default, true, currentPid)
               .WithCareTaker(default, history);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.TestEvictionCallback(knownPnr);

                walker.State.Peer
                   .Should()
                   .Be(lastPid);

                previousState.Neighbours
                   .Select(n => n.PeerIdentifier.PeerId)
                   .Should()
                   .Contain(walker.StateCandidate.Peer.PeerId);
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
               .WithStateCandidate(default, false, _ownNode, default, CorrelationId.GenerateCorrelationId());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(typeof(PeerNeighborsResponse));

                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(subbedDto);
                        });

                    //walker.StateCandidate.Neighbours
                    //   .Received(0)
                    //   .Add(Arg.Any<INeighbour>());
                }
            }
        }

        [Fact]
        public async Task Can_Process_Valid_PeerNeighbourResponse_Message_And_Ping_Provided_Neighbours()
        {
            var pnr = CorrelationId.GenerateCorrelationId();

            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(GetNeighbourResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithStateCandidate(default, false, _ownNode, DiscoveryHelper.MockNeighbours(), pnr);
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(typeof(PeerNeighborsResponse), pnr, _ownNode);
                    
                    var peerNeighborsResponse = new PeerNeighborsResponse();
                   
                    peerNeighborsResponse.Peers.Add(walker.StateCandidate.Neighbours
                       .Select(i => i.PeerIdentifier.PeerId)
                    );
                   
                    subbedDto.Message.Returns(peerNeighborsResponse);
                    
                    discoveryTestBuilder.PeerClientObservables.ToList().ForEach(o =>
                    {
                        o.ResponseMessageSubject.OnNext(subbedDto);
                    });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();
                    
                    walker.DtoFactory
                       .Received(Constants.AngryPirate)
                       .GetDto(
                            Arg.Is(new PingRequest()),
                            Arg.Any<IPeerIdentifier>(),
                            Arg.Any<IPeerIdentifier>()
                        );
                    
                    walker.PeerClient
                       .Received(Constants.AngryPirate)
                       .SendMessage(Arg.Any<IMessageDto<PingRequest>>());
                    
                    walker.StateCandidate.Neighbours
                       .Where(n => n.State == NeighbourState.Contacted)
                       .ToList().Count
                       .Should()
                       .Be(Constants.AngryPirate);
                }
            }
        }
        
        [Fact]
        public async Task Can_Correlate_Known_Ping_And_Update_Neighbour_State()
        {
            var neighbours = DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourState.Contacted, CorrelationId.GenerateCorrelationId());

            var discoveryTestBuilder = DiscoveryTestBuilder.GetDiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(default, typeof(PingResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithStateCandidate(default, false, _ownNode, neighbours, CorrelationId.GenerateCorrelationId());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.Take(5)
                   .SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver))
                {
                    neighbours.ToList().ForEach(n =>
                    {
                        var subbedDto = DiscoveryHelper.SubDto(typeof(PingResponse), n.DiscoveryPingCorrelationId, n.PeerIdentifier);
                        var peerNeighborsResponse = new PingResponse();
                        subbedDto.Message.Returns(peerNeighborsResponse);    
                        
                        discoveryTestBuilder.PeerClientObservables.ToList().ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(subbedDto);
                        });
                    });
                    
                    await TaskHelper.WaitForAsync(() => streamObserver.ReceivedCalls()
                           .Count(c => c.GetMethodInfo().Name == nameof(streamObserver.OnNext)) == neighbours.Count,
                        TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                    await TaskHelper.WaitForAsync(
                        () => walker.StateCandidate.Neighbours.All(n => n.State == NeighbourState.Responsive),
                        TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                    walker.StateCandidate.Neighbours.All(n => n.State == NeighbourState.Responsive).Should().BeTrue();
                }
            }
        }
        
        [Fact(Skip = "for now")]
        public void Known_Evicted_Correlation_Cache_PingRequest_Message_Increments_UnResponsivePeer()
        {
            var pnr = CorrelationId.GenerateCorrelationId();
            
            var evictionEvent = new ReplaySubject<ICorrelationId>(0);

            evictionEvent.OnNext(pnr);
            
            var initialMemento = DiscoveryHelper.SubMemento(_ownNode, 
                DiscoveryHelper.MockDnsClient(_settings)
                   .GetSeedNodesFromDns(_settings.SeedServers)
                   .ToNeighbours()
            );
            
            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer.Returns(initialMemento.Peer);
            initialStateCandidate.Neighbours.Returns(Substitute.For<INeighbours>());
            
            initialStateCandidate.CreateMemento().Returns(initialMemento);
            
            initialStateCandidate.Neighbours.Contains(
                Arg.Any<INeighbour>()).Returns(true);

            initialStateCandidate.PnrCorrelationId.ReturnsForAnyArgs(pnr);

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
               .WithStateCandidate(initialStateCandidate);
            
            using (var walker = discoveryTestBuilder.Build())
            {
                walker.StateCandidate.ReceivedWithAnyArgs(1).Neighbours.Count();
            }
        }
    }
}
