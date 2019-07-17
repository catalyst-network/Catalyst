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

using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.Authentication;
using Nethereum.RLP;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.Rpc.Authentication
{
    /// <summary>
    /// Using SharpRepository to authenticate RPC node operators
    /// </summary>
    /// <seealso cref="IAuthenticationStrategy" />
    public class RepositoryAuthenticationStrategy : IAuthenticationStrategy
    {
        /// <summary>The trusted peers</summary>
        private readonly IRepository<AuthCredentials, string> _trustedPeers;

        /// <summary>Initializes a new instance of the <see cref="RepositoryAuthenticationStrategy"/> class.</summary>
        /// <param name="trustedPeers">The trusted peers.</param>
        public RepositoryAuthenticationStrategy(IRepository<AuthCredentials, string> trustedPeers)
        {
            _trustedPeers = trustedPeers;
        }

        /// <inheritdoc cref="IAuthenticationStrategy"/>
        public bool Authenticate(IPeerIdentifier peerIdentifier)
        {
            return _trustedPeers.TryFind(t =>
                t.IpAddress.Equals(peerIdentifier.Ip.ToString()) &&
                t.PublicKey.Equals(peerIdentifier.PublicKey.ToStringFromRLPDecoded()), out _);
        }
    }
}
