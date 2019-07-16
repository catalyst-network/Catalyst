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
using System.Threading;
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
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Discovery;
using Catalyst.Node.Core.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DnsClient;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Type = System.Type;

namespace Catalyst.Node.Core.UnitTests.P2P.Discovery
{
    public sealed class HastingsDiscoveryTests
    {
        private readonly string _seedPid;
        private readonly List<string> _dnsDomains;
        private readonly PeerSettings _settings;
        private ILookupClient _lookupClient;
        private ILogger _logger;
        private IPeerIdentifier _ownNode;

        public HastingsDiscoveryTests()
        {
            _dnsDomains = new List<string>
            {
                "seed1.catalystnetwork.io",
                "seed2.catalystnetwork.io",
                "seed3.catalystnetwork.io",
                "seed4.catalystnetwork.io",
                "seed5.catalystnetwork.io"
            };
            
            _settings = new PeerSettings(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build());
            
            _logger = Substitute.For<ILogger>();
            _ownNode = PeerIdentifierHelper.GetPeerIdentifier("ownNode");
        }

        [Fact(Skip = "if we have tests for Dns.GetSeedNodesFromDns this seems redundant")]
        public void Discovery_Can_Query_Dns_For_Seed_Nodes()
        {
            using (var walker = new HastingsDiscovery(Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                Substitute.For<IDns>(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                Substitute.For<IEnumerable<IPeerClientObservable>>(),
                0
            ))
            {
                walker.Dns.GetSeedNodesFromDns(Arg.Any<IEnumerable<string>>()).ReceivedWithAnyArgs(1);
            }
        }
        
        [Fact]
        public void Discovery_Can_Build_Initial_State_From_SeedNodes()
        {   
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                Substitute.For<IEnumerable<IPeerClientObservable>>(),
                0
            ))
            {
                walker.State.Peer.PublicKey.ToHex()
                   .Equals("33326b7373683569666c676b336a666d636a7330336c646a346866677338676e");

                walker.State.CurrentPeersNeighbours.Should().HaveCount(5);
            }
        }

        [Theory]
        [InlineData(typeof(PingResponse), typeof(PingResponseObserver), "OnPingResponse")]
        [InlineData(typeof(PeerNeighborsResponse), typeof(GetNeighbourResponseObserver), "OnPeerNeighbourResponse")]
        public async Task Can_Merge_PeerClientObservable_Stream_And_Read_Items_Pushed_On_Separate_Streams(Type discoveryMessage, Type observer, string logMsg)
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                (IPeerClientObservable) Activator.CreateInstance(observer, _logger)
            };

            var initialMemento = HastingDiscoveryHelper.SubMemento(_ownNode, HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings)
               .GetSeedNodesFromDns(_settings.SeedServers).ToList()
            );
            
            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer = initialMemento.Peer;
            var isccpn = initialMemento.Neighbours;
            initialStateCandidate.CurrentPeersNeighbours.Returns(isccpn);
            initialStateCandidate.CreateMemento().Returns(initialMemento);
            var pnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(),
                initialMemento.Peer);
            initialStateCandidate.ExpectedPnr.Returns(pnr);
            
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                peerClientObservers,
                0,
                Substitute.For<IHastingCareTaker>(),
                initialStateCandidate
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = HastingDiscoveryHelper.SubDto(discoveryMessage, initialStateCandidate.ExpectedPnr.Key, initialStateCandidate.ExpectedPnr.Value);

                    peerClientObservers.ToList().ForEach(o => o._responseMessageSubject.OnNext(subbedDto));

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
                    walker.Logger.Received(1).Debug(Arg.Is(logMsg));
                }
            }
        }
        
        [Fact]
        public async Task Can_Correlate_Discovery_Ping_And_Store_Active_Peer_In_Originator()
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new PingResponseObserver(_logger)
            };

            var initialMemento = HastingDiscoveryHelper.SubMemento(_ownNode, 
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings)
                   .GetSeedNodesFromDns(_settings.SeedServers).ToList()
            );
            
            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer = initialMemento.Peer;
            initialStateCandidate.CreateMemento().Returns(initialMemento);
            var expectedResponses = HastingDiscoveryHelper.MockContactedNeighboursValuePairs();
            initialStateCandidate.ContactedNeighbours.Returns(expectedResponses);

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                peerClientObservers,
                0,
                Substitute.For<IHastingCareTaker>(),
                initialStateCandidate
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();
        
                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    expectedResponses.ToList().ForEach(r =>
                    {
                        peerClientObservers.ToList()
                           .ForEach(o =>
                            {
                                o._responseMessageSubject.OnNext(HastingDiscoveryHelper.SubDto(typeof(PingResponse), r.Key, r.Value));
                            });    
                    });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);
        
                    streamObserver
                       .Received(5)
                       .OnNext(Arg.Any<IPeerClientMessageDto>());

                    walker.StateCandidate.CurrentPeersNeighbours
                       .Should()
                       .NotContain(expectedResponses);
                    
                    walker.StateCandidate.ContactedNeighbours
                       .Should()
                       .Contain(expectedResponses);

                    walker.StateCandidate.ContactedNeighbours.Count.Should().Be(5);
                }
            }
        }

        [Fact]
        public async Task Can_Discard_UnKnown_PingResponse()
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new PingResponseObserver(_logger)
            };

            var expectedPingResponse = new List<KeyValuePair<ICorrelationId, IPeerIdentifier>>();

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                peerClientObservers,
                0
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto1 = Substitute.For<IPeerClientMessageDto>();
                    subbedDto1.Sender.Returns(expectedPingResponse.FirstOrDefault().Value);
                    subbedDto1.CorrelationId.Returns(expectedPingResponse.FirstOrDefault().Key);
                    subbedDto1.Message.Returns(new PingResponse());

                    peerClientObservers.ToList().ForEach(o => o._responseMessageSubject.OnNext(subbedDto1));

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);

                    streamObserver.Received(1).OnNext(Arg.Any<IPeerClientMessageDto>());
                    walker.Logger.Received(1).Debug(Arg.Is("OnPingResponse"));
                    
                    walker.StateCandidate.CurrentPeersNeighbours.Count.Should().Be(0);
                }
            }
        }

        [Fact]
        public void Can_Discard_UnKnown_PeerNeighbourResponse_Message()
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new GetNeighbourResponseObserver(_logger)
            };

            var initialMemento = HastingDiscoveryHelper.SubMemento(_ownNode, HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings)
               .GetSeedNodesFromDns(_settings.SeedServers).ToList()
            );
            
            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer = initialMemento.Peer;
            initialStateCandidate.CurrentPeersNeighbours.Returns(Substitute.For<IList<IPeerIdentifier>>());
            initialStateCandidate.CreateMemento().Returns(initialMemento);

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                peerClientObservers,
                0,
                Substitute.For<IHastingCareTaker>(),
                initialStateCandidate
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = HastingDiscoveryHelper.SubDto(typeof(PingResponse));

                    peerClientObservers.ToList().ForEach(o => o._responseMessageSubject.OnNext(subbedDto));

                    walker.StateCandidate.CurrentPeersNeighbours.Received(0).Add(Arg.Any<IPeerIdentifier>());
                }
            }
        }

        [Fact]
        public async Task Can_Process_Expected_PeerNeighbourResponse_Message()
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new GetNeighbourResponseObserver(_logger)
            };

            var contactedNeighboursValuePairs = HastingDiscoveryHelper.MockContactedNeighboursValuePairs();
            
            var subbedDtoFactory = HastingDiscoveryHelper.SubDtoFactory(_ownNode, contactedNeighboursValuePairs, new PingResponse());
            
            var initialMemento = HastingDiscoveryHelper.SubMemento(_ownNode, HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings)
               .GetSeedNodesFromDns(_settings.SeedServers).ToList()
            );
            
            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer = initialMemento.Peer;
            var isccpn = initialMemento.Neighbours;
            initialStateCandidate.CurrentPeersNeighbours.Returns(isccpn);
            initialStateCandidate.CreateMemento().Returns(initialMemento);
            var pnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(),
                initialMemento.Peer);
            initialStateCandidate.ExpectedPnr.Returns(pnr);
            
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                subbedDtoFactory,
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                peerClientObservers,
                0,
                Substitute.For<IHastingCareTaker>(),
                initialStateCandidate,
                initialStateCandidate
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = HastingDiscoveryHelper.SubDto(typeof(PeerNeighborsResponse), pnr.Key, pnr.Value);
                    
                    var peerNeighborsResponse = new PeerNeighborsResponse();
                    peerNeighborsResponse.Peers.Add(contactedNeighboursValuePairs.ToList().Select(kv => kv.Value.PeerId));
                   
                    subbedDto.Message.Returns(peerNeighborsResponse);
                    
                    peerClientObservers.ToList()
                       .ForEach(o =>
                        {
                            o._responseMessageSubject.OnNext(subbedDto);
                        });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);

                    walker.StateCandidate.ContactedNeighbours.Received(5).Add(Arg.Any<KeyValuePair<ICorrelationId, IPeerIdentifier>>());

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
                    
                    walker.StateCandidate.UnreachableNeighbour.Should().Be(0);
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
            
            var initialMemento = HastingDiscoveryHelper.SubMemento(_ownNode, HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings)
               .GetSeedNodesFromDns(_settings.SeedServers).ToList()
            );
            
            var initialStateCandidate = Substitute.For<IHastingsOriginator>();
            initialStateCandidate.Peer = initialMemento.Peer;
            initialStateCandidate.CurrentPeersNeighbours.Returns(Substitute.For<IList<IPeerIdentifier>>());
            
            initialStateCandidate.CreateMemento().Returns(initialMemento);
            
            initialStateCandidate.ContactedNeighbours.Contains(
                Arg.Any<KeyValuePair<ICorrelationId, IPeerIdentifier>>()).Returns(true);

            initialStateCandidate.ExpectedPnr.ReturnsForAnyArgs(pnr);

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                Substitute.For<IList<IPeerClientObservable>>(),
                0,
                Substitute.For<IHastingCareTaker>(),
                initialStateCandidate,
                initialStateCandidate
            ))
            {
                walker.StateCandidate.ReceivedWithAnyArgs(1).IncrementUnreachablePeer();
            }
        }

        [Fact]
        public void Known_Evicted_PeerNeighbourRequest_Walk_Backs_State()
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new GetNeighbourResponseObserver(_logger)
            };

            var pnr = HastingDiscoveryHelper.MockPnr();

            var subbedCareTaker = Substitute.For<IHastingCareTaker>();
            
            var seedState = HastingDiscoveryHelper.GenerateSeedState(_ownNode, _dnsDomains, _settings);
            
            var currentState = HastingDiscoveryHelper.SubMemento();
            var currentOriginator = HastingDiscoveryHelper.MockOriginator(currentState.Peer, currentState.Neighbours);

            var stateCandidate = HastingDiscoveryHelper.SubMemento();
            var stateCandidateOriginator =
                HastingDiscoveryHelper.MockOriginator(stateCandidate.Peer, seedState.Neighbours);

            var mockedHistory = new Stack<IHastingMemento>();
            mockedHistory.Push(seedState);
            subbedCareTaker.HastingMementoList.Returns(mockedHistory);
            subbedCareTaker.Get().Returns(seedState);
            
            var evictionEvent = new ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>>(0);
            evictionEvent.OnNext(pnr);
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                HastingDiscoveryHelper.SubCancellationProvider(),
                peerClientObservers,
                0,
                subbedCareTaker,
                currentOriginator,
                stateCandidateOriginator
            ))
            {
                walker.State.Peer
                   .Should()
                   .Be(currentOriginator.Peer);
                
                walker.StateCandidate.Peer
                   .Should()
                   .Be(stateCandidateOriginator.Peer);

                // walker.StateCandidate
                //    .Received(1)
                //    .ExpectedPnr.Equals(Arg.Is(pnr));
                //
                
                walker._hastingCareTaker.Received(1).Get();

                // walker.State.Received(1).SetMemento(Arg.Any<IHastingMemento>());

                // walker.DtoFactory.Received(1).GetDto(new PeerNeighborsRequest(), Arg.Is(_ownNode),
                // Arg.Any<IPeerIdentifier>());
                // walker.PeerClient.ReceivedWithAnyArgs(1).SendMessage(Arg.Any<IMessageDto<PeerNeighborsRequest>>());
            }
        }
    }
}
