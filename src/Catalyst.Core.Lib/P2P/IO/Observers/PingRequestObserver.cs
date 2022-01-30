#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Protocol.IPPN;
using Dawn;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public sealed class PingRequestObserver 
        : RequestObserverBase<PingRequest, PingResponse>,
            IP2PMessageObserver
    {
        public PingRequestObserver(IPeerSettings peerSettings, 
            IPeerClient peerClient,
            ILogger logger)
            : base(logger, peerSettings, peerClient) {
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
