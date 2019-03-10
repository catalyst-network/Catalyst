/*
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

using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Core.P2P.IO
{
    public class InboundPeerSessionHandler : SessionHandler
    {
        private readonly IInboundPeerSession _inboundSession;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="inboundSession"></param>
        public InboundPeerSessionHandler(ILogger logger, IInboundPeerSession inboundSession)
        {
            _logger = logger;
            _inboundSession = inboundSession;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override async void ChannelActive(IChannelHandlerContext context)
        {
            var channel = context.Channel;
            _logger.Debug("received a new connection from peer {0}", channel.RemoteAddress);
            
            // var challengeRsponseCommand = new ChallengeRsponseCommand();
            
            // var challengeResponse = await SendCommandAsync(connectCommand, channel);
            
            // if (!challengeResponse.IsOk)
            // {
                // return;
            // }

            var peer = new Peer();
            // peer.PeerIdentifier = challengeResponse.peerIdentifier;
            
            //do some store in the database
            
            await _inboundSession.OnConnected(peer, channel);
        }
    }
}