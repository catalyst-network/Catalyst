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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.IO.Observers;
using Catalyst.Protocol.IPPN;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.P2P.IO.Observers
{
    public sealed class PingRequestObserver 
        : RequestObserverBase<PingRequest, PingResponse>,
            IP2PMessageObserver
    {
        public PingRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger)
            : base(logger, peerIdentifier) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pingRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        protected override PingResponse HandleRequest(PingRequest pingRequest, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, ICorrelationId correlationId)
        {
            Guard.Argument(pingRequest, nameof(pingRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            
            Logger.Debug("message content is {0} IP: {1} PeerId: {2}", pingRequest, senderPeerIdentifier.Ip, senderPeerIdentifier.PeerId);

            return new PingResponse();
        }
    }
}
