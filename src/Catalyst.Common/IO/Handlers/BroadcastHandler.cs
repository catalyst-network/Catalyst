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
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Handlers
{
    /// <summary>
    /// Channel Gossip Pipeline
    /// Handles gossip messages
    /// </summary>
    /// <seealso cref="ObservableServiceHandler" />
    public sealed class BroadcastHandler
        : InboundChannelHandlerBase<ProtocolMessage>
    {
        private readonly IBroadcastManager _broadcastManager;

        /// <summary>Initializes a new instance of the <see cref="BroadcastHandler"/> class.</summary>
        /// <param name="broadcastManager">The gossip manager.</param>
        public BroadcastHandler(IBroadcastManager broadcastManager)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            _broadcastManager = broadcastManager;
        }

        /// <summary>
        /// Any broadcast message which is handled by this handler has already been signature checked.
        /// The <see cref="BroadcastHandler"/> will get the original inner message and pass it onto the handler
        /// in-charge of executing the RX handlers.
        /// </summary>
        /// <param name="ctx">The Channel handler context.</param>
        /// <param name="msg">The gossip message.</param>
        protected override void ChannelRead0(IChannelHandlerContext ctx, ProtocolMessage msg)
        {
            if (msg.IsBroadCastMessage())
            {
                Logger.Verbose("Broadcast message {msg} received.", msg);
                var innerGossipMessageSigned = ProtocolMessageSigned.Parser.ParseFrom(msg.Value);
                _broadcastManager.ReceiveAsync(innerGossipMessageSigned)
                   .ConfigureAwait(false).GetAwaiter().GetResult();

                ctx.FireChannelRead(innerGossipMessageSigned.Message);
            }

            if (msg.TypeUrl.Equals(ProtocolMessage.Descriptor.ShortenedFullName()))
            {
                Logger.Verbose("Maybe Broadcast {msg} received.", msg);
                var innerMessage = ProtocolMessage.Parser.ParseFrom(msg.Value);
                if (innerMessage.TypeUrl.EndsWith("Broadcast")) Logger.Warning("message {innerMessage} not seen as broadcast", innerMessage);

                // 🍖🍖🍖🌮🌮🥐🥓🥫🍤☕☕🍍🍒🍒🌳🌳🌿😚😏😏😘🥰😛🙃🤓👾🙀😺🐭🍗

                if (innerMessage.TypeUrl == CandidateDeltaBroadcast.Descriptor.ShortenedFullName())
                {
                    ctx.FireChannelRead(innerMessage.FromProtocolMessage<CandidateDeltaBroadcast>());
                }

                if (innerMessage.TypeUrl == FavouriteDeltaBroadcast.Descriptor.ShortenedFullName())
                {
                    ctx.FireChannelRead(innerMessage.FromProtocolMessage<FavouriteDeltaBroadcast>());
                }

                if (innerMessage.TypeUrl == DeltaDfsHashBroadcast.Descriptor.ShortenedFullName())
                {
                    ctx.FireChannelRead(innerMessage.FromProtocolMessage<DeltaDfsHashBroadcast>());
                }

                // 🚲🚃🚘🚚🚇🚦⛩👩‍🦰👩‍🦰🤴👮‍♂️👨‍🏭👨‍🍳🧛‍♂️🧝‍♂️👰🤰🧛‍♀️💁‍♀️🕺🕺🚶‍♂️👩‍🦯🤸‍♀️👏🙌👱‍♀️👼👨‍💻

                ctx.FireChannelRead(innerMessage);
            }
            else
            {
                Logger.Verbose("Message {msg} was not a broadcast message.", msg);
                ctx.FireChannelRead(msg);
            }
        }
    }
}
