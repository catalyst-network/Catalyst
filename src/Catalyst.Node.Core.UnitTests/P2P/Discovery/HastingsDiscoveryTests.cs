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
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Discovery;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DnsClient;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Nethereum.Hex.HexConvertors.Extensions;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.Discovery
{
    public sealed class HastingsDiscoveryTests
    {
        private IDns _dns;
        private string _seedPid;
        private List<string> _dnsDomains;
        private readonly PeerSettings _settings;
        private ILookupClient _lookupClient;
        private IPeerIdValidator _peerIdValidator;
        private readonly PeerId _ownPeerId;
        private PeerId _testPeer1;

        public HastingsDiscoveryTests()
        {
            _ownPeerId = PeerIdHelper.GetPeerId("own_node");
            _testPeer1 = PeerIdHelper.GetPeerId("test_peer1");
            
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
        }

        [Fact]
        public void Discovery_Can_Query_Dns_For_Seed_Nodes()
        {   
            using (var walker = new HastingsDiscovery(Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                Substitute.For<IDns>(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                SubCancellationProvider(),
                Substitute.For<IChangeTokenProvider>()
            ))
            {
                walker.Dns.GetSeedNodesFromDns(Arg.Any<IEnumerable<string>>()).Received(1);
            }
        }
        
        [Fact]
        public void Discovery_Can_Build_Initial_State_From_SeedNodes()
        {   
            using (var walker = new HastingsDiscovery(Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                SubDnsClient(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                SubCancellationProvider(),
                Substitute.For<IChangeTokenProvider>()
            ))
            {
                walker.state.Peer.PublicKey.ToHex()
                   .Equals("33326b7373683569666c676b336a666d636a7330336c646a346866677338676e");

                walker.state.CurrentPeersNeighbours.Should().HaveCount(5);
            }
        }

        [Fact]
        public async Task Can_Merge_PeerClientObservable_Stream_And_Read_Items_Pushed_On_Separate_Streams()
        {
            using (var walker = new HastingsDiscovery(Substitute.For<ILogger>(),
                Substitute.For<IRepository<Peer>>(),
                Substitute.For<IDns>(),
                _settings,
                Substitute.For<IPeerClient>(),
                Substitute.For<IDtoFactory>(),
                SubCancellationProvider(),
                Substitute.For<IChangeTokenProvider>()
            ))
            {
                var discoveryMessageType1 = new ReplaySubject<IPeerClientMessageDto>(1);
                var discoveryMessage1Stream = discoveryMessageType1.AsObservable();
                discoveryMessage1Stream.SubscribeOn(TaskPoolScheduler.Default).Subscribe((reputationChange) => Substitute.For<ILogger>());

                var discoveryMessageType2 = new ReplaySubject<IPeerClientMessageDto>(1);
                var discoveryMessage2Stream = discoveryMessageType2.AsObservable();
                discoveryMessage2Stream.SubscribeOn(TaskPoolScheduler.Default).Subscribe((reputationChange) => Substitute.For<ILogger>());

                walker.MergeDiscoveryMessageStreams(discoveryMessage1Stream);
                walker.MergeDiscoveryMessageStreams(discoveryMessage2Stream);
                
                var streamObserver = Substitute.For<IObserver<IPeerClientMessageDto>>();

                using (walker.DiscoveryStream.SubscribeOn(TaskPoolScheduler.Default)
                   .Subscribe(streamObserver.OnNext))
                {
                    var subbedDto1 = Substitute.For<IPeerClientMessageDto>();
                    subbedDto1.Sender.Returns(PeerIdentifierHelper.GetPeerIdentifier("sender1"));
                    discoveryMessageType1.OnNext(subbedDto1);
                    
                    var subbedDto2 = Substitute.For<IPeerClientMessageDto>();
                    subbedDto2.Sender.Returns(PeerIdentifierHelper.GetPeerIdentifier("sender2"));
                    discoveryMessageType2.OnNext(subbedDto2);

                    await walker.DiscoveryStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync(2);

                    streamObserver.Received(2).OnNext(Arg.Any<IPeerClientMessageDto>());
                }
            }
        }

        private IDtoFactory SubDtoFactoryGetDtoResponse()
        {
            var dtoFactory = Substitute.For<IDtoFactory>();
            var subbedOwnNodePid = Substitute.For<IPeerIdentifier>();
            subbedOwnNodePid.PeerId.Returns(_ownPeerId);
            
            var subbedTestPid = Substitute.For<IPeerIdentifier>();
            subbedTestPid.PeerId.Returns(_testPeer1);

            dtoFactory.GetDto(new PeerNeighborsRequest(),
                subbedOwnNodePid,
                subbedTestPid);

            return dtoFactory;
        }

        private ICancellationTokenProvider SubCancellationProvider(bool result = false)
        {
            var provider = Substitute.For<ICancellationTokenProvider>();
            provider.HasTokenCancelled().Returns(result);
            return provider;
        }

        private IDns SubDnsClient(bool peerIdValidatorResponse = true)
        {
            _peerIdValidator = Substitute.For<IPeerIdValidator>();
            _lookupClient = Substitute.For<ILookupClient>();

            _dnsDomains.ForEach(domain =>
            {
                MockQueryResponse.CreateFakeLookupResult(domain, _seedPid, _lookupClient);
            });

            return new Common.Network.DevDnsClient(_settings);
        }
    }
}
