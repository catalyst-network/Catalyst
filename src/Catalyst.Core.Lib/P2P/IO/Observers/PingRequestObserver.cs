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
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Protocol.IPPN;
using Catalyst.Protocol.Peer;
using Dawn;
using DotNetty.Transport.Channels;
using Serilog;
using System;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class PingRequestObserver 
        : RequestObserverBase<PingRequest, PingResponse>,
            IP2PMessageObserver
    {
        private readonly IPeerRepository _peerRepository;
        private readonly SyncState _syncState;
        public PingRequestObserver(IPeerSettings peerSettings, 
            IPeerRepository peerRepository,
            SyncState syncState,
            ILogger logger)
            : base(logger, peerSettings) {
            _peerRepository = peerRepository;
            _syncState = syncState;
        }
        
        /// <summary>
        ///     Basic method to handle ping messages. 
        /// </summary>
        /// <param name="pingRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerId"></param>
        /// <param name="correlationId"></param>
        /// <returns><see cref="PingResponse"/></returns>
        protected override PingResponse HandleRequest(PingRequest pingRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerId,
            ICorrelationId correlationId)
        {
            Guard.Argument(pingRequest, nameof(pingRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();

            Guard.Argument(senderPeerId, nameof(senderPeerId)).NotNull();
            
            Logger.Debug("message content is {0} IP: {1} PeerId: {2}", pingRequest, senderPeerId.Ip, senderPeerId);

            var peer = _peerRepository.Get(senderPeerId);
            if (peer == null)
            {
                _peerRepository.Add(new Peer
                {
                    PeerId = senderPeerId,
                    LastSeen = DateTime.UtcNow
                });
            }
            else
            {
                peer.LastSeen = DateTime.UtcNow;
            }

            return new PingResponse() { Height = _syncState.HighestBlock, IsSync = _syncState.IsSynchronized } ;
        }
    }
}
