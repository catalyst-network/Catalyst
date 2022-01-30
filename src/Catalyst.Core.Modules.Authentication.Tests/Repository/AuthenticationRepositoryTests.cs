#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Authentication.Models;
using Catalyst.Core.Modules.Authentication.Repository;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using SharpRepository.InMemoryRepository;
using NUnit.Framework;
using MultiFormats;

namespace Catalyst.Core.Modules.Authentication.Tests.Repository
{
    public sealed class AuthenticationRepositoryTests
    {
        private readonly IAuthenticationStrategy _repositoryAuthenticationStrategy;
        private readonly MultiAddress _trustedPeer;

        public AuthenticationRepositoryTests()
        {
            _trustedPeer = MultiAddressHelper.GetAddress("Trusted");
            var whiteListRepo = new AuthCredentialRepository(new InMemoryRepository<AuthCredentials, string>());

            whiteListRepo.Add(new AuthCredentials
            {
                Address = _trustedPeer.ToString()
            });

            _repositoryAuthenticationStrategy = new RepositoryAuthenticationStrategy(whiteListRepo);
        }

        [Test]
        public void Can_Validate_Trusted_Peer()
        {
            _repositoryAuthenticationStrategy.Authenticate(_trustedPeer).Should().BeTrue();
        }

        [Test]
        public void Can_Invalidate_Untrusted_Peer()
        {
            _repositoryAuthenticationStrategy.Authenticate(MultiAddressHelper.GetAddress("NotTrusted"))
               .Should().BeFalse();
        }
    }
}
