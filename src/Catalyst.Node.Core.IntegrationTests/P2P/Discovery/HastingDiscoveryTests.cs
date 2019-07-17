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
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.P2P.IO;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Discovery;
using Catalyst.Node.Core.P2P.IO.Messaging.Correlation;
using Catalyst.Node.Core.P2P.IO.Messaging.Dto;
using Catalyst.Node.Core.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.IntegrationTests.P2P.Discovery
{
    public sealed class HastingDiscoveryTests
    {
        private List<string> _dnsDomains;
        private PeerSettings _settings;
        private ILogger _logger;
        private IPeerIdentifier _ownNode;

        public HastingDiscoveryTests()
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

        [Fact]
        public async Task Evicted_Known_Ping_Message_Sets_Contacted_Neighbour_As_UnReachable() { }

        [Fact]
        public async Task Expected_Ping_Response_Sets_Neighbour_As_Reachable()
        {
            var pr = new PingResponseObserver(Substitute.For<ILogger>());
            var peerClientObservers = new List<IPeerClientObservable> {pr};

            var seedState = HastingDiscoveryHelper.GenerateSeedState(_ownNode, _dnsDomains, _settings);
            var seedOrigin = new HastingsOriginator();
            seedOrigin.SetMemento(seedState);

            var stateCareTaker = new HastingCareTaker();
            var stateHistory = new Stack<IHastingMemento>();
            stateHistory.Push(seedState);
            
            HastingDiscoveryHelper.GenerateMementoHistory(stateHistory, 5).ToList().ForEach(i => stateCareTaker.Add(i));
            
            var knownPnr = HastingDiscoveryHelper.MockPnr();
            var stateCandidate = HastingDiscoveryHelper.MockOriginator();
            stateCandidate.ExpectedPnr = knownPnr;
            stateCandidate.CurrentPeersNeighbours.Clear();

            using (var walker = new HastingsDiscovery(
                Substitute.For<ILogger>(),
                new InMemoryRepository<Peer>(), 
                HastingDiscoveryHelper.SubDnsClient(_dnsDomains, _settings),
                _settings,
                Substitute.For<IPeerClient>(),
                new DtoFactory(),
                new PeerMessageCorrelationManager(
                    Substitute.For<IReputationManager>(),
                    Substitute.For<IMemoryCache>(),
                    Substitute.For<ILogger>(),
                    new TtlChangeTokenProvider(3)),
                new CancellationTokenProvider(),
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
                        stateCandidate.ContactedNeighbours.FirstOrDefault().Value,
                        stateCandidate.ContactedNeighbours.FirstOrDefault().Key
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
