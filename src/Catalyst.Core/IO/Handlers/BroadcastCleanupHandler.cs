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
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Extensions;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.IO.Handlers
{
    public class BroadcastCleanupHandler : InboundChannelHandlerBase<ProtocolMessage>
    {
        private readonly IBroadcastManager _broadcastManager;

        public BroadcastCleanupHandler(IBroadcastManager broadcastManager)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            _broadcastManager = broadcastManager;
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessage msg)
        {
            _broadcastManager.RemoveSignedBroadcastMessageData(msg.CorrelationId.ToCorrelationId());
            ctx.FireChannelRead(msg);
        }
    }
}
