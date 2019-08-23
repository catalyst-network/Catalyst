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
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Common.P2P.Discovery;
using Catalyst.Common.P2P.Models;
using Catalyst.Common.Types;
using Catalyst.Common.Util;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Protocol.Common;
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
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStepProposal()
               .WithCurrentStep()
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
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithStepProposal()
               .WithCurrentStep()
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

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithCurrentStep(default, true, knownStepPid)
               .WithStepProposal(default,
                    true,
                    knownNextCandidate,
                    DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourStateTypes.Responsive))
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.CurrentStep.Peer
                   .Should()
                   .Be(knownStepPid);
                
                walker.WalkForward();
                
                walker.CurrentStep.Peer
                   .Should()
                   .Be(knownNextCandidate);
            }
        }

        [Fact]
        public void Can_Not_WalkForward_With_InValid_Candidate()
        {
            var proposalCandidateId = PeerIdentifierHelper.GetPeerIdentifier("these_eyes....");

            var knownStepPid =
                PeerIdentifierHelper.GetPeerIdentifier("hey_its_jimmys_brother_the_guy_with_the_beautiful_voice");
            var knownStepNeighbours = new Neighbours(new[] {new Neighbour(proposalCandidateId)});
            var latestStep = new HastingsMemento(knownStepPid, knownStepNeighbours);

            var proposal = Substitute.For<IHastingsOriginator>();
            var unresponsiveNeighbours = DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourStateTypes.UnResponsive);
            proposal.Neighbours.Returns(unresponsiveNeighbours);
            proposal.Peer.Returns(proposalCandidateId);

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithCareTaker()
               .WithCurrentStep(latestStep)
               .WithStepProposal(proposal)
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.CurrentStep.Peer
                   .Should()
                   .Be(knownStepPid);
                
                walker.WalkForward();
                
                walker.CurrentStep.Peer
                   .Should()
                   .Be(knownStepPid);
            }
        }

        [Fact]
        public void HasValidCandidate_Can_Validate_Correct_State()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithCareTaker()
               .WithCurrentStep(default,
                    true,
                    default,
                    DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourStateTypes.Responsive))
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.StepProposal.HasValidCandidate().Should().BeTrue();
            }
        }

        [Fact]
        public void HasValidCandidate_Can_Detect_Invalid_State()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithCurrentStep()
               .WithStepProposal(default,
                    true,
                    default,
                    DiscoveryHelper.MockNeighbours(0))
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.StepProposal.HasValidCandidate().Should().BeFalse();
            }
        }
        
        [Fact]
        public void Can_Throw_Exception_In_WalkBack_When_Last_State_Has_No_Neighbours_To_Continue_Walk_Forward()
        {
            var ctp = new CancellationTokenProvider();
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, false)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider(ctp)
               .WithPeerClientObservables()
               .WithCurrentStep()
               .WithStepProposal()
               .WithAutoStart(false)
               .WithBurn(0);

            using (var walker = discoveryTestBuilder.Build())
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    walker.DiscoveryAsync().GetAwaiter().GetResult();
                    Thread.Sleep(2);
                    ctp.CancellationTokenSource.Cancel();
                });
            }
        }

        [Fact]
        public void Can_Get_State_From_CareTaker()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, false)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker();

            using (var walker = discoveryTestBuilder.Build())
            {
                var lastState = walker.HastingsCareTaker.Get();
                lastState.Peer.Should().BeAssignableTo<IPeerIdentifier>();
            }
        }

        [Fact]
        public void Discovery_Can_Build_Initial_State_From_SeedNodes()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.CurrentStep.Peer.PublicKey.ToHex()
                   .Equals("33326b7373683569666c676b336a666d636a7330336c646a346866677338676e");

                walker.StepProposal.Neighbours
                   .Should()
                   .HaveCount(Constants.AngryPirate); // http://giphygifs.s3.amazonaws.com/media/9MFsKQ8A6HCN2/giphy.gif
            }
        }

        [Theory]
        [InlineData(typeof(PingResponse), typeof(PingResponseObserver), "OnPingResponse")]
        [InlineData(typeof(PeerNeighborsResponse), typeof(GetNeighbourResponseObserver), "OnPeerNeighbourResponse")]
        public async Task Can_Merge_PeerClientObservable_Stream_And_Read_Items_Pushed_On_Separate_Streams(Type discoveryMessage, Type observer, string logMsg)
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder();
            var subSeedOriginator = DiscoveryHelper.SubSeedOriginator(_ownNode, _settings);
            discoveryTestBuilder
               .WithDns()
               .WithPeerClientObservables(observer)
               .WithCurrentStep(subSeedOriginator.CreateMemento())
               .WithStepProposal(subSeedOriginator, false, default, default, CorrelationId.GenerateCorrelationId());

            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(discoveryMessage, walker.StepProposal.PnrCorrelationId, walker.StepProposal.Peer);

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
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(PingResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker()
               .WithStepProposal(default, false, _ownNode, default, CorrelationId.GenerateCorrelationId());
            
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
                    
                    walker.StepProposal.Neighbours.Count.Should().Be(0);
                }
            }
        }

        [Fact]
        public void Unknown_Pnr_Message_Does_Not_Walk_Back()
        {
            var candidatePid = PeerIdentifierHelper.GetPeerIdentifier("candidate");
            var currentPid = PeerIdentifierHelper.GetPeerIdentifier("current");

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker()
               .WithCurrentStep(default, true, currentPid)
               .WithStepProposal(default, false, candidatePid, default);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.TestEvictionCallback(CorrelationId.GenerateCorrelationId());

                walker.CurrentStep.Peer
                   .Should()
                   .Be(currentPid);

                walker.StepProposal.Peer
                   .Should()
                   .Be(candidatePid);
            }
        }
        
        [Fact]
        public void Evicted_Known_Pnr_Message_Does_Walk_Back()
        {
            var currentPid = PeerIdentifierHelper.GetPeerIdentifier("current");
            var lastPid = PeerIdentifierHelper.GetPeerIdentifier("last");
            var mockNeighbours = DiscoveryHelper.MockNeighbours(4, NeighbourStateTypes.Responsive)
               .Concat(new[] {new Neighbour(currentPid, NeighbourStateTypes.Contacted, CorrelationId.GenerateEmptyCorrelationId())});
            var previousState = DiscoveryHelper.SubMemento(lastPid, new Neighbours(mockNeighbours));
            var history = new Stack<IHastingsMemento>();
            history.Push(previousState);

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCareTaker(default, history)
               .WithStepProposal(default, true, currentPid, default);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.TestEvictionCallback(walker.StepProposal.PnrCorrelationId);

                walker.CurrentStep.Peer
                   .Should()
                   .Be(lastPid);

                previousState.Neighbours
                   .Select(n => n.PeerIdentifier.PeerId)
                   .Should()
                   .Contain(walker.StepProposal.Peer.PeerId);
            }
        }

        [Fact]
        public void Can_Discard_UnKnown_PeerNeighbourResponse_Message()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCurrentStep()
               .WithStepProposal(default, false, _ownNode, default, CorrelationId.GenerateCorrelationId());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = GetPeerNeighbourResponse(walker, DiscoveryHelper.MockNeighbours());
                    ((PeerNeighborsResponse) subbedDto.Message).Peers.Count.Should().BeGreaterThan(0);

                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(subbedDto);
                        });

                    walker.StepProposal.Neighbours.Count.Should().Be(0);
                }
            }
        }

        [Fact]
        public async Task Can_Process_Valid_PeerNeighbourResponse_Message_And_Ping_Provided_Neighbours()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker()
               .WithStepProposal(default, true, _ownNode, DiscoveryHelper.MockNeighbours());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = GetPeerNeighbourResponse(walker, walker.StepProposal.Neighbours);

                    discoveryTestBuilder.PeerClientObservables.AsParallel().ForAll(o =>
                    {
                        o.ResponseMessageSubject.OnNext(subbedDto);
                    });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();
                    
                    walker.DtoFactory
                       .Received(Constants.AngryPirate)
                       .GetDto(
                            Arg.Any<ProtocolMessage>(),
                            Arg.Any<IPeerIdentifier>()
                        );
                    
                    walker.PeerClient
                       .Received(Constants.AngryPirate)
                       .SendMessage(Arg.Any<IMessageDto<ProtocolMessage>>());
                    
                    walker.StepProposal.Neighbours
                       .Where(n => n.StateTypes == NeighbourStateTypes.Contacted)
                       .ToList().Count
                       .Should()
                       .Be(Constants.AngryPirate);
                }
            }
        }

        private IPeerClientMessageDto GetPeerNeighbourResponse(DiscoveryTestBuilder.HastingDiscoveryTest walker, 
            INeighbours neighbours)
        {
            var subbedDto = DiscoveryHelper.SubDto(
                typeof(PeerNeighborsResponse),
                walker.StepProposal.PnrCorrelationId,
                _ownNode);

            var peerNeighborsResponse = new PeerNeighborsResponse();

            peerNeighborsResponse.Peers.Add(neighbours
               .Select(i => i.PeerIdentifier.PeerId)
            );

            subbedDto.Message.Returns(peerNeighborsResponse);
            return subbedDto;
        }

        [Fact]
        public async Task Can_Correlate_Known_Ping_And_Update_Neighbour_State()
        {
            var neighbours = DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourStateTypes.Contacted, CorrelationId.GenerateCorrelationId());

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(PingResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithCurrentStep()
               .WithStepProposal(default, false, _ownNode, neighbours, CorrelationId.GenerateCorrelationId());
            
            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.Take(5)
                   .SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver))
                {
                    neighbours.AsParallel().ForAll(n =>
                    {
                        var subbedDto = DiscoveryHelper.SubDto(typeof(PingResponse), n.DiscoveryPingCorrelationId, n.PeerIdentifier);
                        var peerNeighborsResponse = new PingResponse();
                        subbedDto.Message.Returns(peerNeighborsResponse);    
                        
                        discoveryTestBuilder.PeerClientObservables.ToList().ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(subbedDto);
                        });
                    });

                    await TaskHelper.WaitForAsync(
                        () => walker.StepProposal.Neighbours.All(n => n.StateTypes == NeighbourStateTypes.Responsive),
                        TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                    walker.StepProposal.Neighbours.Count(n => n.StateTypes == NeighbourStateTypes.Responsive).Should().Be(neighbours.Count);
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

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart(false)
               .WithBurn(0)
               .WithCurrentStep(initialMemento)
               .WithStepProposal(initialStateCandidate);
            
            using (var walker = discoveryTestBuilder.Build())
            {
                walker.StepProposal.ReceivedWithAnyArgs(1).Neighbours.Count();
            }
        }
    }
}
