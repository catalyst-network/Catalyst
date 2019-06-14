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

using Catalyst.Common.Interfaces.Rpc.Authentication;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Handlers
{
    /// <summary>
    /// DotNetty Handler in-charge of blocking RPC messages if the node operator is not trusted
    /// </summary>
    /// <seealso cref="SimpleChannelInboundHandler{ProtocolMessageSigned}" />
    public class AuthenticationHandler : SimpleChannelInboundHandler<ProtocolMessage>
    {
        /// <summary>The authentication strategy</summary>
        private readonly IAuthenticationStrategy _authenticationStrategy;

        /// <summary>Initializes a new instance of the <see cref="AuthenticationHandler"/> class.</summary>
        /// <param name="authenticationStrategy">The authentication strategy.</param>
        public AuthenticationHandler(IAuthenticationStrategy authenticationStrategy)
        {
            _authenticationStrategy = authenticationStrategy;
        }

        /// <inheritdoc cref="SimpleChannelInboundHandler{I}"/>>
        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessage msg)
        {
            if (_authenticationStrategy.Authenticate(new PeerIdentifier(msg.PeerId)))
            {
                ctx.FireChannelRead(msg);
            }
        }
    }
}
