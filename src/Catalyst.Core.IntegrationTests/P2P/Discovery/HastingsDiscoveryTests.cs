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
using Autofac;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P.Discovery.Hastings;
using Catalyst.Core.P2P.IO.Messaging.Dto;
using Catalyst.Core.P2P.IO.Observers;
using Catalyst.Core.UnitTests.P2P.Discovery;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.P2P.Discovery
{
    public sealed class HastingsDiscoveryTests : ConfigFileBasedTest
    {
        public HastingsDiscoveryTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Protocol.Common.Network.Devnet))
        }, output)
        {
            _testScheduler = new TestScheduler();
            _settings = PeerSettingsHelper.TestPeerSettings();
            _ownNode = PeerIdentifierHelper.GetPeerIdentifier("ownNode");

            ContainerProvider.ConfigureContainerBuilder(true, true);
            _logger = ContainerProvider.Container.Resolve<ILogger>();
        }

        private readonly TestScheduler _testScheduler;
        private readonly IPeerSettings _settings;
        private readonly IPeerIdentifier _ownNode;
        private readonly ILogger _logger;

        [Fact]
#pragma warning disable 1998
        public async Task Evicted_Known_Ping_Message_Sets_Contacted_Neighbour_As_UnReachable_And_Can_RollBack_State()
#pragma warning restore 1998
        {
            var cacheEntriesByRequest = new Dictionary<ByteString, ICacheEntry>();

            var seedState = DiscoveryHelper.MockSeedState(_ownNode, _settings);
            var seedOrigin = HastingsOriginator.Default;
            seedOrigin.RestoreMemento(seedState);
            var stateCareTaker = new HastingsCareTaker();
            var stateHistory = new Stack<IHastingsMemento>();
            stateHistory.Push(seedState);

            stateHistory =
                DiscoveryHelper.MockMementoHistory(stateHistory, 5); //this isn't an angry pirate this is just 5

            stateHistory.Last().Neighbours.First().StateTypes = NeighbourStateTypes.Responsive;

            stateHistory.ToList().ForEach(i => stateCareTaker.Add(i));

            var stateCandidate = DiscoveryHelper.MockOriginator(default,
                DiscoveryHelper.MockNeighbours(Constants.AngryPirate, NeighbourStateTypes.NotContacted));

            var memoryCache = Substitute.For<IMemoryCache>();
            var correlatableMessages = new List<CorrelatableMessage<ProtocolMessage>>();
            var peerMessageCorrelationManager =
                DiscoveryHelper.MockCorrelationManager(_testScheduler, default, memoryCache, logger: _logger);

            _logger.Debug("Seed StepProposal has peerId {peerIdentifier}", _ownNode);
            _logger.Debug("StepProposal has peerId {peerIdentifier}", stateCandidate.Peer);

            stateCandidate.Neighbours.ToList().ForEach(n =>
            {
                _logger.Debug("Setting up neighbour {neighbour}", n.PeerIdentifier);
                _logger.Debug("Adding eviction callbacks expectation for correlationId {correlationId}",
                    n.DiscoveryPingCorrelationId);

                cacheEntriesByRequest = CacheHelper.MockCacheEvictionCallback(
                    n.DiscoveryPingCorrelationId.Id.ToByteString(),
                    memoryCache,
                    cacheEntriesByRequest
                );
                _logger.Debug("Create ping request with correlationId {correlationId} and sender {sender}",
                    n.DiscoveryPingCorrelationId, stateCandidate.Peer);
                var msg = new CorrelatableMessage<ProtocolMessage>

                {
                    Content = new PingRequest().ToProtocolMessage(_ownNode.PeerId, n.DiscoveryPingCorrelationId),
                    Recipient = n.PeerIdentifier
                };

                correlatableMessages.Add(msg);
                peerMessageCorrelationManager.AddPendingRequest(msg);
            });

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger(_logger)
               .WithScheduler(_testScheduler)
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(PingResponseObserver))
               .WithPeerMessageCorrelationManager(peerMessageCorrelationManager)
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker(stateCareTaker)
               .WithStepProposal(stateCandidate);

            _logger.Information("Building Hasting discovery for test.");
            using (var walker = discoveryTestBuilder.Build())
            {
                stateCandidate.Neighbours.AsParallel().ForAll(n =>
                {
                    cacheEntriesByRequest[n.DiscoveryPingCorrelationId.Id.ToByteString()]
                       .PostEvictionCallbacks[0]
                       .EvictionCallback
                       .Invoke(
                            n.PeerIdentifier,
                            correlatableMessages.Single(i =>
                                i.Recipient.PeerId.Equals(n.PeerIdentifier.PeerId)),
                            EvictionReason.Expired,
                            new object()
                        );
                });

                _testScheduler.Start();

                var success = stateCandidate.Neighbours.All(n => n.StateTypes == NeighbourStateTypes.UnResponsive);
                _logger.Verbose("Succeeded waiting for neighbour state change to Unresponsive? {success}", success);

                walker.StepProposal.Neighbours
                   .Count(n => n.StateTypes == NeighbourStateTypes.UnResponsive)
                   .Should()
                   .Be(Constants.AngryPirate);

                walker.StepProposal.HasValidCandidate()
                   .Should()
                   .BeFalse();

                walker.HastingsCareTaker.HastingMementoList.TryPeek(out var expectedCurrentState);

                walker.WalkBack();

                walker.CurrentStep.Peer
                   .Should()
                   .Be(expectedCurrentState.Peer);
            }
        }

        [Fact]
#pragma warning disable 1998
        public async Task Expected_Ping_Response_From_All_Contacted_Nodes_Produces_Valid_State_Candidate()
#pragma warning restore 1998
        {
            var seedState = DiscoveryHelper.SubSeedState(_ownNode, _settings);

            var stateCareTaker = new HastingsCareTaker();
            var stateHistory = new Stack<IHastingsMemento>();
            stateHistory.Push(seedState);

            DiscoveryHelper.MockMementoHistory(stateHistory, Constants.AngryPirate).ToList()
               .ForEach(i => stateCareTaker.Add(i));

            var knownPnr = CorrelationId.GenerateCorrelationId();
            var stateCandidate = DiscoveryHelper.SubOriginator(expectedPnr: knownPnr);

            var discoveryTestBuilder = new DiscoveryTestBuilder()
               .WithLogger(_logger)
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(PingResponseObserver))
               .WithPeerMessageCorrelationManager()
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker(stateCareTaker)
               .WithStepProposal(stateCandidate);

            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                IList<IPeerClientMessageDto> dtoList = new List<IPeerClientMessageDto>();

                stateCandidate.Neighbours.ToList().ForEach(i =>
                {
                    var dto = new PeerClientMessageDto(new PingResponse(),
                        stateCandidate.Neighbours.FirstOrDefault()?.PeerIdentifier,
                        stateCandidate.Neighbours.FirstOrDefault()?.DiscoveryPingCorrelationId
                    );

                    dtoList.Add(dto);

                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o => { o.ResponseMessageSubject.OnNext(dto); });
                });

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    walker.StepProposal.Neighbours
                       .Where(n => n.StateTypes == NeighbourStateTypes.Responsive)
                       .Select(i => i.PeerIdentifier.PeerId)
                       .Should()
                       .BeSubsetOf(
                            stateCandidate.Neighbours
                               .Select(i => i.PeerIdentifier.PeerId)
                        );
                }
            }
        }

        [Fact]
#pragma warning disable 1998
        public async Task Expected_Ping_Response_Sets_Neighbour_As_Reachable()
#pragma warning restore 1998
        {
            var seedState = DiscoveryHelper.SubSeedState(_ownNode, _settings);
            var seedOrigin = HastingsOriginator.Default;
            seedOrigin.RestoreMemento(seedState);

            var stateCareTaker = new HastingsCareTaker();
            var stateHistory = new Stack<IHastingsMemento>();
            stateHistory.Push(seedState);

            DiscoveryHelper.MockMementoHistory(stateHistory, 5) //this isn't an angry pirate this is just 5
               .ToList()
               .ForEach(i => stateCareTaker.Add(i));

            var stateCandidate = DiscoveryHelper.MockOriginator();

            var discoveryTestBuilder = new DiscoveryTestBuilder();
            discoveryTestBuilder
               .WithLogger(_logger)
               .WithPeerRepository()
               .WithDns(default, true)
               .WithPeerSettings()
               .WithPeerClient()
               .WithCancellationProvider()
               .WithPeerClientObservables(typeof(PingResponseObserver))
               .WithPeerMessageCorrelationManager()
               .WithAutoStart()
               .WithBurn()
               .WithCareTaker(stateCareTaker)
               .WithStepProposal(stateCandidate);

            using (var walker = discoveryTestBuilder.Build())
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.Subscribe(streamObserver.OnNext))
                {
                    var pingDto = new PeerClientMessageDto(new PingResponse(),
                        stateCandidate.Neighbours.FirstOrDefault()?.PeerIdentifier,
                        stateCandidate.Neighbours.FirstOrDefault()?.DiscoveryPingCorrelationId
                    );

                    discoveryTestBuilder.PeerClientObservables
                       .ToList()
                       .ForEach(o => { o.ResponseMessageSubject.OnNext(pingDto); });

                    _testScheduler.Start();

                    streamObserver.Received(1).OnNext(Arg.Is(pingDto));

                    walker.StepProposal.Neighbours
                       .Where(n => n.StateTypes == NeighbourStateTypes.Responsive)
                       .Select(n => n.PeerIdentifier)
                       .Contains(pingDto.Sender);
                }
            }
        }
    }
}
