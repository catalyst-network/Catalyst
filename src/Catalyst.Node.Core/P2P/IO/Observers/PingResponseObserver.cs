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

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Node.Core.P2P.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Core.P2P.IO.Observers
{
    public sealed class PingResponseObserver
        : ResponseObserverBase<PingResponse>,
            IP2PMessageObserver
    {
        private readonly ReplaySubject<IPeerClientMessageDto<PingResponse>> _pingResponse;
        public IObservable<IPeerClientMessageDto<PingResponse>> PingResponseStream => _pingResponse.AsObservable();

        public PingResponseObserver(ILogger logger)
            : base(logger)
        {
            _pingResponse = new ReplaySubject<IPeerClientMessageDto<PingResponse>>(0);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pingResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(PingResponse pingResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            _pingResponse.OnNext(new PeerClientMessageDto<PingResponse>(pingResponse, senderPeerIdentifier));
        }
    }
}
