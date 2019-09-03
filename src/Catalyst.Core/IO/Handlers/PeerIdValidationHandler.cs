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
using Catalyst.Abstractions.P2P;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.IO.Handlers
{
    public sealed class PeerIdValidationHandler : SimpleChannelInboundHandler<ProtocolMessageSigned>
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IPeerIdValidator _peerIdValidator;
        public PeerIdValidationHandler(IPeerIdValidator peerIdValidator) { _peerIdValidator = peerIdValidator; }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessageSigned msg)
        {
            Logger.Verbose("Received {msg}", msg);
            if (_peerIdValidator.ValidatePeerIdFormat(msg.Message.PeerId))
            {
                ctx.FireChannelRead(msg);
            }
        }
    }
}
