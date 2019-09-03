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

using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Core.Rpc.Authentication;
using Catalyst.Core.Rpc.Authentication.Models;
using Catalyst.Core.Rpc.Authentication.Repository;
using Catalyst.Core.Util;
using Catalyst.TestUtils;
using FluentAssertions;
using SharpRepository.InMemoryRepository;
using Xunit;

namespace Catalyst.Node.POA.CE.UnitTests.Repository
{
    public sealed class AuthenticationRepositoryTests
    {
        private readonly IAuthenticationStrategy _repositoryAuthenticationStrategy;
        private readonly IPeerIdentifier _trustedPeer;

        public AuthenticationRepositoryTests()
        {
            _trustedPeer = PeerIdentifierHelper.GetPeerIdentifier("Trusted");
            var whiteListRepo = new AuthCredentialRepository(new InMemoryRepository<AuthCredentials, string>());

            whiteListRepo.Add(new AuthCredentials
            {
                PublicKey = _trustedPeer.PublicKey.KeyToString(),
                IpAddress = _trustedPeer.Ip.ToString()
            });

            _repositoryAuthenticationStrategy = new RepositoryAuthenticationStrategy(whiteListRepo);
        }

        [Fact]
        public void Can_Validate_Trusted_Peer()
        {
            _repositoryAuthenticationStrategy.Authenticate(_trustedPeer).Should().BeTrue();
        }

        [Fact]
        public void Can_Invalidate_Untrusted_Peer()
        {
            _repositoryAuthenticationStrategy.Authenticate(PeerIdentifierHelper.GetPeerIdentifier("NotTrusted"))
               .Should().BeFalse();
        }
    }
}
