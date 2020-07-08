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
using Lib.P2P.Protocols;
using MultiFormats;
using Serilog;
using System;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class PingRequestObserver 
        : RequestObserverBase<PingRequest, PingResponse>,
            IP2PMessageObserver
    {
        private readonly IPeerRepository _peerRepository;
        public PingRequestObserver(IPeerSettings peerSettings, 
            IPeerRepository peerRepository,
            IPeerClient peerClient,
            ILogger logger)
            : base(logger, peerSettings, peerClient) {
            _peerRepository = peerRepository;
        }
        
        /// <summary>
        ///     Basic method to handle ping messages. 
        /// </summary>
        /// <param name="pingRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="sender"></param>
        /// <param name="correlationId"></param>
        /// <returns><see cref="PingResponse"/></returns>
        protected override PingResponse HandleRequest(PingRequest pingRequest,
            MultiAddress sender,
            ICorrelationId correlationId)
        {
            Guard.Argument(pingRequest, nameof(pingRequest)).NotNull();
            Guard.Argument(sender, nameof(sender)).NotNull();

            return new PingResponse();
        }
    }
}
