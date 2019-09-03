
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
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Catalyst.Core.IO.Observers;
using Catalyst.Core.P2P.IO.Messaging.Dto;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Core.P2P.IO.Observers
{
    public sealed class GetNeighbourResponseObserver
        : ResponseObserverBase<PeerNeighborsResponse>,
            IP2PMessageObserver,
            IPeerClientObservable
    {
        public ReplaySubject<IPeerClientMessageDto> ResponseMessageSubject { get; }
        public IObservable<IPeerClientMessageDto> MessageStream => ResponseMessageSubject.AsObservable();
        
        public GetNeighbourResponseObserver(ILogger logger) : base(logger)
        {
            ResponseMessageSubject = new ReplaySubject<IPeerClientMessageDto>(1);
        }

        /// <summary>
        ///     Processes a GetNeighbourResponse item from stream.
        /// </summary>
        /// <param name="messageDto"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(PeerNeighborsResponse messageDto,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            ResponseMessageSubject.OnNext(new PeerClientMessageDto(messageDto, senderPeerIdentifier, correlationId));
        }
    }
}
