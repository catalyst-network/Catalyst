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

using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P.Messaging.Gossip;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Inbound.Handlers
{
    /// <summary>
    /// Channel Gossip Pipeline
    /// Handles gossip messages
    /// </summary>
    /// <seealso cref="ObservableServiceHandler" />
    public sealed class GossipHandler
        : SimpleChannelInboundHandler<ProtocolMessage>
    {
        private readonly IGossipManager _gossipManager;

        /// <summary>Initializes a new instance of the <see cref="GossipHandler"/> class.</summary>
        /// <param name="gossipManager">The gossip manager.</param>
        public GossipHandler(IGossipManager gossipManager)
        {
            _gossipManager = gossipManager;
        }

        /// <summary>
        /// Any gossip message which is handled by this handler has already been signature checked.
        /// The GossipHandler will get the original inner message and pass it onto the handler
        /// in-charge of executing the RX handlers.
        /// </summary>
        /// <param name="ctx">The Channel handler context.</param>
        /// <param name="msg">The gossip message.</param>
        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessage msg)
        {
            if (msg.CheckIfMessageIsBroadcast())
            {
                _gossipManager.ReceiveAsync(msg).ConfigureAwait(false).GetAwaiter().GetResult();

                ProtocolMessage originalGossipedMessage = ProtocolMessage.Parser.ParseFrom(msg.Value);
                ctx.FireChannelRead(originalGossipedMessage);
            }
            else
            {
                ctx.FireChannelRead(msg);
            }
        }
    }
}
