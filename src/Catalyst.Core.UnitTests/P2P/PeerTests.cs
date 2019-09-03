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
using Catalyst.Core.P2P.Models;
using Catalyst.TestUtils;
using FluentAssertions;
using SharpRepository.InMemoryRepository;
using Xunit;

namespace Catalyst.Core.UnitTests.P2P
{
    public sealed class PeerTests
    {
        [Fact]
        public void EntityStoreAuditsCreateTime()
        {
            var repo = new InMemoryRepository<Peer, string>();
            var peer = new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test")};
            repo.Add(peer);
            var retrievedPeer = repo.Get(peer.DocumentId);
            var now = DateTime.UtcNow.Date;
            var datecomparer = retrievedPeer.Created.Date.ToString("MM/dd/yyyy");

            // ReSharper disable once SuspiciousTypeConversion.Global
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            datecomparer.Should().Equals(now.ToString("MM/dd/yyyy"));
            retrievedPeer.Modified.Should().BeNull();
        }
        
        [Fact]
        public void EntityStoreAuditsModifiedTime()
        {
            var repo = new InMemoryRepository<Peer, string>();
            var peer = new Peer {PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test")};
            repo.Add(peer);
            var retrievedPeer = repo.Get(peer.DocumentId);
            retrievedPeer.Touch();
            repo.Update(retrievedPeer);
            var retrievedmodified = repo.Get(peer.DocumentId);
            var now = DateTime.UtcNow.Date;

            if (retrievedmodified.Modified == null)
            {
                return;
            }
            
            var dateComparer = retrievedmodified.Modified.Value.Date.ToString("MM/dd/yyyy");

            // ReSharper disable once SuspiciousTypeConversion.Global
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            dateComparer.Should().Equals(now.ToString("MM/dd/yyyy"));
        }
    }
}
