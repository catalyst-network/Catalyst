#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Protocol.Peer;

namespace Catalyst.Core.Modules.Authentication
{
    /// <summary>
    ///     For node runners who don't like to use protection.
    /// </summary>
    /// <seealso cref="IAuthenticationStrategy" />
    public class NoAuthenticationStrategy : IAuthenticationStrategy
    {
        /// <inheritdoc cref="IAuthenticationStrategy"/>
        public bool Authenticate(PeerId peerIdentifier) { return true; }
    }
}
