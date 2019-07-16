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
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Discovery;
using Catalyst.Node.Core.P2P.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DnsClient;
using FluentAssertions;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Nethereum.Hex.HexConvertors.Extensions;
using Nito.Comparers.Linq;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ReceivedExtensions;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Xunit.Extensions;
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
            
            _seedPid = "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839";
            
            _settings = new PeerSettings(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build());
            
            _logger = Substitute.For<ILogger>();
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
                SubCancellationProvider(),
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
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
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
            
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
                peerClientObservers,
                0
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto1 = Substitute.For<IPeerClientMessageDto>();
                    subbedDto1.Sender.Returns(PeerIdentifierHelper.GetPeerIdentifier($"sender"));
                    subbedDto1.Message.Returns(Activator.CreateInstance(discoveryMessage));
                    
                    peerClientObservers.ToList().ForEach(o => o._responseMessageSubject.OnNext(subbedDto1));

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);

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

            var expectedPingResponse = new List<KeyValuePair<ICorrelationId, IPeerIdentifier>>
            {
                new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(),
                    PeerIdentifierHelper.GetPeerIdentifier("neighbour"))
            };

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
                peerClientObservers,
                expectedPingResponse,
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
                    walker.Cache.Should().NotContain(expectedPingResponse.FirstOrDefault());
                    
                    walker.State.CurrentPeersNeighbours
                       .ToList()
                       .Where(i => Equals(new PeerIdentifier(i.PeerId),
                            expectedPingResponse.FirstOrDefault().Value))
                       .Select(id => new PeerIdentifier(id.PeerId))
                       .FirstOrDefault()
                       .Should()
                       .BeSameAs(
                            expectedPingResponse.FirstOrDefault().Value
                        );

                    walker.State.ExpectedResponses.Should().Be(0);
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
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
                peerClientObservers,
                expectedPingResponse,
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
                    
                    walker.Cache.Count.Should().Be(0);
                    walker.StateCandidate.CurrentPeersNeighbours.Count.Should().Be(0);
                }
            }
        }

        [Theory]
        [InlineData(typeof(PeerNeighborsResponse), typeof(GetNeighbourResponseObserver))]
        public void Can_Discard_UnKnown_PeerNeighbourResponse_Message(Type discoveryMessage, Type observer)
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new GetNeighbourResponseObserver(_logger)
            };

            var expectedPeerNeighbourResponse = new KeyValuePair<ICorrelationId, IPeerIdentifier>();

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
                peerClientObservers,
                Substitute.For<IList<KeyValuePair<ICorrelationId, IPeerIdentifier>>>(),
                0
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto1 = Substitute.For<IPeerClientMessageDto>();
                    subbedDto1.Sender.Returns(expectedPeerNeighbourResponse.Value);
                    subbedDto1.CorrelationId.Returns(expectedPeerNeighbourResponse.Key);
                    subbedDto1.Message.Returns(new PeerNeighborsResponse());
                    
                    peerClientObservers.ToList().ForEach(o => o._responseMessageSubject.OnNext(subbedDto1));

                    walker.Cache.ReceivedWithAnyArgs(1);
                    walker.Cache.Received(0).Remove(Arg.Any<KeyValuePair<ICorrelationId, IPeerIdentifier>>());
                    walker.State.ExpectedResponses.Should().Be(0);
                    walker.State.UnreachableNeighbour.Should().Be(0);
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

            var neighbours = new List<PeerId>
            {
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32))
            };

            var expectedPeerNeighbourResponse = new List<KeyValuePair<ICorrelationId, IPeerIdentifier>>();

            var ownNode = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var subbedPnr =
                new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(), ownNode);
            
            expectedPeerNeighbourResponse.Add(subbedPnr);
                
            neighbours.ForEach(n =>
            {
                expectedPeerNeighbourResponse.Add(new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(),
                    new PeerIdentifier(n)));
            });

            var subbedDtoFactory = Substitute.For<IDtoFactory>();
            
            neighbours.ToList().ForEach(n =>
            {
                subbedDtoFactory.GetDto(Arg.Any<PingRequest>(),
                    ownNode,
                    new PeerIdentifier(n)
                ).Returns(
                    new MessageDto<PingRequest>(new PingRequest(),
                        ownNode,
                        new PeerIdentifier(n)
                    )
                );
            });
            
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                subbedDtoFactory,
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
                peerClientObservers,
                expectedPeerNeighbourResponse,
                0
            ))
            {
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto = Substitute.For<IPeerClientMessageDto>();
                    subbedDto.Sender.Returns(ownNode);
                    subbedDto.CorrelationId.Returns(subbedPnr.Key);
                    
                    var peerNeighborsResponse = new PeerNeighborsResponse();
                    peerNeighborsResponse.Peers.Add(neighbours);
                   
                    subbedDto.Message.Returns(peerNeighborsResponse);
                    
                    peerClientObservers.ToList()
                       .ForEach(o =>
                        {
                            o._responseMessageSubject.OnNext(subbedDto);
                        });

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(1);

                    walker.StateCandidate.ExpectedResponses
                       .Should()
                       .Be(neighbours.Count);
                    
                    walker.Cache
                       .Select(i => i.Value)
                       .Where(i => neighbours.Contains(i.PeerId))
                       .Should()
                       .HaveCount(5);

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
                    
                    walker.StateCandidate.ExpectedResponses.Should().Be(neighbours.Count);
                    walker.StateCandidate.UnreachableNeighbour.Should().Be(0);
                }
            }
        }

        [Fact]
        public void Known_Evicted_Correlation_Cache_PingRequest_Message_Increments_UnResponsivePeer()
        {
            var subbedCache = Substitute.For<IList<KeyValuePair<ICorrelationId, IPeerIdentifier>>>();
            
            var pnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(),
                PeerIdentifierHelper.GetPeerIdentifier("sender")
            );
            
            subbedCache.Contains(Arg.Any<KeyValuePair<ICorrelationId, IPeerIdentifier>>()).Returns(true);

            var evictionEvent = new ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>>(0);

            evictionEvent.OnNext(pnr);
            
            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
                Substitute.For<IList<IPeerClientObservable>>(),
                subbedCache,
                0
            ))
            {
                walker.Cache
                   .ReceivedWithAnyArgs(1)
                   .Contains(pnr);
                
                walker.Cache
                   .ReceivedWithAnyArgs(1)
                   .Remove(pnr);

                walker.StateCandidate.UnreachableNeighbour
                   .Should()
                   .Be(1);
            }
        }

        [Fact]
        public async Task Known_Evicted_PeerNeighbourRequest_Walk_Backs_State()
        {
            var peerClientObservers = new List<IPeerClientObservable>
            {
                new GetNeighbourResponseObserver(_logger)
            };
            
            var pnr = new KeyValuePair<ICorrelationId, IPeerIdentifier>(CorrelationId.GenerateCorrelationId(),
                PeerIdentifierHelper.GetPeerIdentifier("sender")
            );
            
            var subbedCache = Substitute.For<IList<KeyValuePair<ICorrelationId, IPeerIdentifier>>>();
            subbedCache.Contains(Arg.Is(new KeyValuePair<ICorrelationId, IPeerIdentifier>(pnr.Key, pnr.Value)))
               .Returns(true);
            subbedCache.Contains(Arg.Is(new KeyValuePair<ICorrelationId, IPeerIdentifier>(pnr.Key, pnr.Value)))
               .Returns(true);

            var evictionEvent = new ReplaySubject<KeyValuePair<ICorrelationId, IPeerIdentifier>>(0);

            var subbedHastingCareTaker = Substitute.For<IHastingCareTaker>();
            var subbedMemento = Substitute.For<IHastingMemento>();
            subbedMemento.Peer.Returns(PeerIdentifierHelper.GetPeerIdentifier("previous_step_peer"));
            
            var neighbours = new List<PeerId>
            {
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32)),
                PeerIdHelper.GetPeerId(ByteUtil.GenerateRandomByteArray(32))
            };

            subbedMemento.Neighbours.Returns(new ConcurrentBag<IPeerIdentifier>(neighbours.Select(i => new PeerIdentifier(i))));
            subbedHastingCareTaker.Get().ReturnsForAnyArgs(subbedMemento);

            using (var walker = new HastingsDiscovery(_logger,
                Substitute.For<IRepository<Peer>>(),
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                Substitute.For<IPeerMessageCorrelationManager>(),
                SubCancellationProvider(),
                peerClientObservers,
                subbedCache,
                0,
                subbedHastingCareTaker
            ))
            {
                walker.StateCandidate.Peer = pnr.Value;
                evictionEvent.OnNext(pnr);
                walker.State.Peer.Should().Be(subbedMemento.Peer);
            }
        }

        private static ICancellationTokenProvider SubCancellationProvider(bool result = false)
        {
            var provider = Substitute.For<ICancellationTokenProvider>();
            provider.HasTokenCancelled().Returns(result);
            return provider;
        }
        
        private IDns SubDnsClient(bool peerIdValidatorResponse = true)
        {
            _lookupClient = Substitute.For<ILookupClient>();
        
            _dnsDomains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, _seedPid, _lookupClient);
            });
        
            return new Common.Network.DevDnsClient(_settings);
        }
    }
}
