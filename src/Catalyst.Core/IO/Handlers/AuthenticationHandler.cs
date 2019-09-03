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

using System.Reflection;
using System.Security.Authentication;
using Catalyst.Abstractions.Rpc.Authentication;
using Catalyst.Core.P2P;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.IO.Handlers
{
    /// <summary>
    /// DotNetty Handler in-charge of blocking RPC messages if the node operator is not trusted
    /// </summary>
    /// <seealso cref="SimpleChannelInboundHandler{I}" />
    public sealed class AuthenticationHandler : InboundChannelHandlerBase<ProtocolMessageSigned>
    {
        /// <summary>The authentication strategy</summary>
        private readonly IAuthenticationStrategy _authenticationStrategy;

        /// <summary>Initializes a new instance of the <see cref="AuthenticationHandler"/> class.</summary>
        /// <param name="authenticationStrategy">The authentication strategy.</param>
        public AuthenticationHandler(IAuthenticationStrategy authenticationStrategy) 
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            _authenticationStrategy = authenticationStrategy;
        }

        /// <inheritdoc cref="SimpleChannelInboundHandler{I}"/>>
        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessageSigned msg)
        {
            if (_authenticationStrategy.Authenticate(new PeerIdentifier(msg.Message.PeerId)))
            {
                ctx.FireChannelRead(msg);
            }
            else
            {
                ctx.CloseAsync();

                throw new AuthenticationException("Authentication Attempt Failed");
            }
        }
    }
}
