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
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.P2P.Discovery;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Modules.P2P.Discovery.Hastings.Tests.UnitTests
{
    public sealed class HastingsDiscoveryTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly IPeerSettings _settings;
        private readonly PeerId _ownNode;

        public HastingsDiscoveryTests()
        {
            _testScheduler = new TestScheduler();
            _settings = PeerSettingsHelper.TestPeerSettings();
            _ownNode = PeerIdHelper.GetPeerId("ownNode");
        }

        [Test]
        public void Can_Store_Peer_After_Burn_In()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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

                Enumerable.Range(0, 5).ToList().ForEach(i => { walker.TestStorePeer(Substitute.For<INeighbour>()); });

                walker.PeerRepository
                   .Received(5)
                   .Add(Arg.Any<Peer>());
            }
        }

        [Test]
        public void Cant_Store_Peer_During_Burn_In()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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

        [Test]
        public void Can_WalkForward_With_Valid_Candidate()
        {
            var knownStepPid =
                PeerIdHelper.GetPeerId("hey_its_jimmys_brother_the_guy_with_the_beautiful_voice");
            var knownNextCandidate =
                PeerIdHelper.GetPeerId("these_eyes....");

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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
                    DiscoveryHelper.MockNeighbours(Constants.NumberOfRandomPeers, NeighbourStateTypes.Responsive))
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

        [Test]
        public void Can_Not_WalkForward_With_InValid_Candidate()
        {
            var proposalCandidateId = PeerIdHelper.GetPeerId("these_eyes....");

            var knownStepPid =
                PeerIdHelper.GetPeerId("hey_its_jimmys_brother_the_guy_with_the_beautiful_voice");
            var knownStepNeighbours = new Neighbours(new[] {new Neighbour(proposalCandidateId)});
            var latestStep = new HastingsMemento(knownStepPid, knownStepNeighbours);

            var proposal = Substitute.For<IHastingsOriginator>();
            var unresponsiveNeighbours =
                DiscoveryHelper.MockNeighbours(Constants.NumberOfRandomPeers, NeighbourStateTypes.UnResponsive);
            proposal.Neighbours.Returns(unresponsiveNeighbours);
            proposal.Peer.Returns(proposalCandidateId);

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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

        [Test]
        public void HasValidCandidate_Can_Validate_Correct_State()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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
                    DiscoveryHelper.MockNeighbours(Constants.NumberOfRandomPeers, NeighbourStateTypes.Responsive))
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.StepProposal.HasValidCandidate().Should().BeTrue();
            }
        }

        [Test]
        public void HasValidCandidate_Can_Detect_Invalid_State()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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

        [Test]
        public async Task Can_Throw_Exception_In_WalkBack_When_Last_State_Has_No_Neighbours_To_Continue_Walk_Forward()
        {
            var ctp = new CancellationTokenProvider(true);
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider(ctp)
               .WithPeerClientObservables()
               .WithCurrentStep()
               .WithStepProposal()
               .WithAutoStart()
               .WithBurn();

            using (var walker = discoveryTestBuilder.Build())
            {
                Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await walker.DiscoveryAsync();
                    Thread.Sleep(2);
                    ctp.CancellationTokenSource.Cancel();
                });
            }
        }

        [Test]
        public void Can_Get_State_From_CareTaker()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker();

            using (var walker = discoveryTestBuilder.Build())
            {
                var lastState = walker.HastingsCareTaker.Get();
                lastState.Peer.Should().NotBeNull();
            }
        }

        [Test]
        public void Discovery_Can_Build_Initial_State_From_SeedNodes()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables()
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker();

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.StepProposal.Neighbours
                   .Should()
                   .HaveCount(Constants
                       .NumberOfRandomPeers); // http://giphygifs.s3.amazonaws.com/media/9MFsKQ8A6HCN2/giphy.gif
            }
        }

        [Theory]
        [TestCase(typeof(PingResponse), typeof(PingResponseObserver), "OnPingResponse")]
        [TestCase(typeof(PeerNeighborsResponse), typeof(GetNeighbourResponseObserver), "OnPeerNeighbourResponse")]
        public void Can_Merge_PeerClientObservable_Stream_And_Read_Items_Pushed_On_Separate_Streams(Type discoveryMessage,
            Type observer,
            string logMsg)
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder();
            var subSeedOriginator = DiscoveryHelper.SubSeedOriginator(_ownNode, _settings);
            discoveryTestBuilder
               .WithScheduler(_testScheduler)
               .WithDns()
               .WithPeerClientObservables(observer)
               .WithCurrentStep(subSeedOriginator.CreateMemento())
               .WithStepProposal(subSeedOriginator, false, default, default, CorrelationId.GenerateCorrelationId());

            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = DiscoveryHelper.SubDto(discoveryMessage, walker.StepProposal.PnrCorrelationId,
                        walker.StepProposal.Peer);

                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o => { o.ResponseMessageSubject.OnNext(subbedDto); });

                    _testScheduler.Start();

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
                }
            }
        }

        [Test]
        public void Can_Discard_UnKnown_PingResponse()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(PingResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker()
               .WithStepProposal(default, false, _ownNode, default, CorrelationId.GenerateCorrelationId());

            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.Subscribe(streamObserver.OnNext))
                {
                    var subbedDto1 = Substitute.For<IPeerClientMessageDto>();
                    subbedDto1.Sender.Returns(PeerIdHelper.GetPeerId());
                    subbedDto1.CorrelationId.Returns(Substitute.For<ICorrelationId>());
                    subbedDto1.Message.Returns(new PingResponse());

                    discoveryTestBuilder.PeerClientObservables.ToList()
                       .ForEach(o => o.ResponseMessageSubject.OnNext(subbedDto1));

                    _testScheduler.Start();

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());

                    walker.StepProposal.Neighbours.Count.Should().Be(0);
                }
            }
        }

        [Test]
        public void Unknown_Pnr_Message_Does_Not_Walk_Back()
        {
            var candidatePid = PeerIdHelper.GetPeerId("candidate");
            var currentPid = PeerIdHelper.GetPeerId("current");

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker()
               .WithCurrentStep(default, true, currentPid)
               .WithStepProposal(default, false, candidatePid);

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

        [Test]
        public void Evicted_Known_Pnr_Message_Does_Walk_Back()
        {
            var currentPid = PeerIdHelper.GetPeerId("current");
            var lastPid = PeerIdHelper.GetPeerId("last");
            var mockNeighbours = DiscoveryHelper.MockNeighbours(4, NeighbourStateTypes.Responsive)
               .Concat(new[]
                {
                    new Neighbour(currentPid, NeighbourStateTypes.Contacted, CorrelationId.GenerateEmptyCorrelationId())
                });
            var previousState = DiscoveryHelper.SubMemento(lastPid, new Neighbours(mockNeighbours));
            var history = new Stack<IHastingsMemento>();
            history.Push(previousState);

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns()
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker(default, history)
               .WithStepProposal(default, true, currentPid);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.TestEvictionCallback(walker.StepProposal.PnrCorrelationId);

                walker.CurrentStep.Peer
                   .Should()
                   .Be(lastPid);

                previousState.Neighbours
                   .Select(n => n.PeerId)
                   .Should()
                   .Contain(walker.StepProposal.Peer);
            }
        }

        [Test]
        public void Can_Discard_UnKnown_PeerNeighbourResponse_Message()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart()
               .WithBurn()
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
                       .ForEach(o => { o.ResponseMessageSubject.OnNext(subbedDto); });

                    walker.StepProposal.Neighbours.Count.Should().Be(0);
                }
            }
        }

        [Test]
        public void Can_Process_Valid_PeerNeighbourResponse_Message_And_Ping_Provided_Neighbours()
        {
            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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

                    _testScheduler.Start();

                    walker.PeerClient
                       .Received(Constants.NumberOfRandomPeers)
                       .SendMessage(Arg.Any<IMessageDto<ProtocolMessage>>());

                    walker.StepProposal.Neighbours
                       .Where(n => n.StateTypes == NeighbourStateTypes.Contacted)
                       .ToList().Count
                       .Should()
                       .Be(Constants.NumberOfRandomPeers);
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
               .Select(i => i.PeerId)
            );

            subbedDto.Message.Returns(peerNeighborsResponse);
            return subbedDto;
        }

        [Test]
        public void Can_Correlate_Known_Ping_And_Update_Neighbour_State()
        {
            var neighbours = DiscoveryHelper.MockNeighbours(Constants.NumberOfRandomPeers,
                NeighbourStateTypes.Contacted, CorrelationId.GenerateCorrelationId());

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
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
                        var subbedDto = DiscoveryHelper.SubDto(typeof(PingResponse), n.DiscoveryPingCorrelationId,
                            n.PeerId);
                        var peerNeighborsResponse = new PingResponse();
                        subbedDto.Message.Returns(peerNeighborsResponse);

                        discoveryTestBuilder.PeerClientObservables.ToList().ForEach(o =>
                        {
                            o.ResponseMessageSubject.OnNext(subbedDto);
                        });
                    });

                    _testScheduler.Start();

                    walker.StepProposal.Neighbours.Count(n => n.StateTypes == NeighbourStateTypes.Responsive).Should()
                       .Be(neighbours.Count);
                }
            }
        }

        [Test]
        public void Known_Evicted_Correlation_Cache_PingRequest_Message_Increments_UnResponsivePeer()
        {
            var pnr = CorrelationId.GenerateCorrelationId();
            var unresponsiveNeighbour = new Neighbour(new PeerId(), NeighbourStateTypes.NotContacted, pnr);

            var initialMemento = DiscoveryHelper.SubMemento(_ownNode,
                DiscoveryHelper.MockDnsClient(_settings)
                   .GetSeedNodesFromDnsAsync(_settings.SeedServers)
                   .ConfigureAwait(false)
                   .GetAwaiter()
                   .GetResult()
                   .ToNeighbours()
            );

            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer.Returns(initialMemento.Peer);
            initialStateCandidate.Neighbours.Returns(new Neighbours(new INeighbour[] {unresponsiveNeighbour}));

            initialStateCandidate.CreateMemento().Returns(initialMemento);

            initialStateCandidate.PnrCorrelationId.ReturnsForAnyArgs(CorrelationId.GenerateCorrelationId());

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger()
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerMessageCorrelationManager()
               .WithPeerClientObservables(typeof(GetNeighbourResponseObserver))
               .WithAutoStart()
               .WithBurn()
               .WithCurrentStep(initialMemento)
               .WithStepProposal(initialStateCandidate);

            using (var walker = discoveryTestBuilder.Build())
            {
                walker.TestEvictionCallback(pnr);
                walker.StepProposal.Neighbours
                   .Count(n => n.StateTypes == NeighbourStateTypes.UnResponsive &&
                        n.DiscoveryPingCorrelationId.Equals(pnr))
                   .Should().Be(1);
            }
        }
    }
}
