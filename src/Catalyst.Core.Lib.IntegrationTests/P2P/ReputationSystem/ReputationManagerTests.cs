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

using System.IO;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Config;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.P2P;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.IntegrationTests.P2P.ReputationSystem
{
    public sealed class ReputationManagerTests : ConfigFileBasedTest
    {
        private readonly IReputationManager _reputationManager;

        public ReputationManagerTests(ITestOutputHelper output) : base(output)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            
            ConfigureContainerBuilder(config);
            
            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                _reputationManager = container.Resolve<IReputationManager>();
            }
        }

        private Peer SavePeerInRepo(IPeerIdentifier pid, int initialRep = 100)
        {
            var subbedPeer = new Peer
            {
                PeerIdentifier = pid,
                Reputation = initialRep
            };
            _reputationManager.PeerRepository.Add(subbedPeer);
            return subbedPeer;
        }

        [Fact]
        public void Can_Save_Increased_Peer()
        {
            var pid = PeerIdentifierHelper.GetPeerIdentifier("some_peer");

            var savedPeer = SavePeerInRepo(pid);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerIdentifier.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(100);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(200);
        }
        
        [Fact]
        public void Can_Save_Decreased_Peer()
        {
            var pid = PeerIdentifierHelper.GetPeerIdentifier("some_peer");

            var savedPeer = SavePeerInRepo(pid);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerIdentifier.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(-100);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(0);
        }
        
        [Fact]
        public void Can_Save_Decreased_Peer_To_Negative_Number()
        {
            var pid = PeerIdentifierHelper.GetPeerIdentifier("some_peer");

            var savedPeer = SavePeerInRepo(pid);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerIdentifier.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(-200);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(-100);
        }
        
        [Fact]
        public void Can_Save_Increased_Peer_From_Negative_Number_To_Positive_Number()
        {
            var pid = PeerIdentifierHelper.GetPeerIdentifier("some_peer");

            var savedPeer = SavePeerInRepo(pid, -100);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerIdentifier.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(200);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(100);
        }
    }
}
