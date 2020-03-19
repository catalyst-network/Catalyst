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

using Autofac;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.P2P.ReputationSystem;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.ReputationSystem;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.IntegrationTests.P2P.ReputationSystem
{
    public sealed class ReputationManagerTests : FileSystemBasedTest
    {
        private readonly IReputationManager _reputationManager;
        private readonly ILifetimeScope _scope;

        public ReputationManagerTests() : base(TestContext.CurrentContext)
        {
            ContainerProvider.ConfigureContainerBuilder();

            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            _reputationManager = _scope.Resolve<IReputationManager>();
        }

        private Peer SavePeerInRepo(PeerId pid, int initialRep = 100)
        {
            var subbedPeer = new Peer
            {
                PeerId = pid,
                Reputation = initialRep
            };
            _reputationManager.PeerRepository.Add(subbedPeer);
            return subbedPeer;
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Can_Save_Increased_Peer()
        {
            var pid = PeerIdHelper.GetPeerId("some_peer");

            var savedPeer = SavePeerInRepo(pid);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerId.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(100);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(200);
        }
        
        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Can_Save_Decreased_Peer()
        {
            var pid = PeerIdHelper.GetPeerId("some_peer");

            var savedPeer = SavePeerInRepo(pid);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerId.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(-100);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(0);
        }
        
        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Can_Save_Decreased_Peer_To_Negative_Number()
        {
            var pid = PeerIdHelper.GetPeerId("some_peer");

            var savedPeer = SavePeerInRepo(pid);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerId.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(-200);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(-100);
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public void Can_Save_Increased_Peer_From_Negative_Number_To_Positive_Number()
        {
            var pid = PeerIdHelper.GetPeerId("some_peer");

            var savedPeer = SavePeerInRepo(pid, -100);
            var peerReputationChange = Substitute.For<IPeerReputationChange>();
            peerReputationChange.PeerId.Returns(pid);
            peerReputationChange.ReputationEvent.Returns(Substitute.For<IReputationEvents>());
            peerReputationChange.ReputationEvent.Amount.Returns(200);
            _reputationManager.OnNext(peerReputationChange);
            var updatedSubbedPeer = _reputationManager.PeerRepository.Get(savedPeer.DocumentId);
            updatedSubbedPeer.Reputation.Should().Be(100);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scope?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
